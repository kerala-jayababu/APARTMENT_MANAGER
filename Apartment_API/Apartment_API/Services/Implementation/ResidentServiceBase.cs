using Apartment_API.Data;
using Apartment_API.DTO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

/// <summary>Shared helpers for resident services (person-type resolution, uploads).</summary>
public abstract class ResidentServiceBase(AppDbContext db, IWebHostEnvironment env)
{
    protected AppDbContext Db { get; } = db;
    protected IWebHostEnvironment Env { get; } = env;

    protected async Task<int> RequirePersonTypeIdAsync(string code, CancellationToken ct)
    {
        var c = code.ToUpperInvariant();
        var id = await Db.PersonTypes.AsNoTracking()
            .Where(p => p.IsActive)
            .Where(p => p.PersonTypeCode.ToUpper() == c)
            .Select(p => p.IdPersonType)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (id == 0)
            id = await Db.PersonTypes.AsNoTracking()
                .Where(p => p.IsActive)
                // EF cannot translate Contains(..., StringComparison); use upper-case match instead.
                .Where(p => p.PersonTypeName.ToUpper().Contains(c))
                .Select(p => p.IdPersonType)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        if (id == 0) throw new InvalidOperationException($"PersonType not found for {code}.");
        return id;
    }

    protected async Task<int> RequireRoleIdAsync(string roleCode, CancellationToken ct = default)
    {
        var code = roleCode.Trim();
        var id = await Db.AppRoles.AsNoTracking()
            .Where(r => r.IsActive && r.RoleCode == code)
            .Select(r => r.IdRole)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (id == 0)
        {
            var roles = await Db.AppRoles.AsNoTracking()
                .Where(r => r.IsActive)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var match = roles.FirstOrDefault(r =>
                string.Equals(r.RoleCode, code, StringComparison.OrdinalIgnoreCase));
            if (match is not null) id = match.IdRole;
        }
        if (id == 0)
            throw new InvalidOperationException($"App role not found: {code}.");
        return id;
    }

    protected string UploadFile(Stream s, int apartmentId, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext)) ext = ".bin";
        var rel = Path.Combine("documents", apartmentId.ToString(), $"{Guid.NewGuid():N}{ext}");
        var full = Path.Combine(Env.ContentRootPath, "Uploads", rel);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using var fs = File.Create(full);
        s.CopyTo(fs);
        return "/uploads/" + rel.Replace("\\", "/");
    }

    protected async Task<int> RequireActiveDocumentCategoryIdAsync(
        int documentCategoryId, CancellationToken cancellationToken = default)
    {
        if (documentCategoryId <= 0)
            throw new InvalidOperationException("documentCategoryId is required and must be a positive value.");

        var category = await Db.DocumentCategories.AsNoTracking()
            .Where(c => c.IdDocumentCategory == documentCategoryId)
            .Select(c => new { c.IsActive, c.CategoryName })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
            throw new InvalidOperationException(
                $"documentCategoryId {documentCategoryId} is not a valid document category.");

        if (!category.IsActive)
            throw new InvalidOperationException(
                $"documentCategoryId {documentCategoryId} ({category.CategoryName.Trim()}) is inactive and cannot be used.");

        return documentCategoryId;
    }

    protected static string NormalizeVehicleNumber(string vehicleNumber) =>
        vehicleNumber.Trim().Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();

    /// <summary>
    /// Rejects duplicate numbers in the request and numbers already registered to another active resident in the apartment.
    /// </summary>
    protected async Task ValidateVehicleNumbersAsync(
        int apartmentId,
        int? excludePersonId,
        IReadOnlyList<VehicleRequestItem>? vehicles,
        CancellationToken cancellationToken = default)
    {
        if (vehicles is not { Count: > 0 }) return;

        var normalizedInRequest = new List<(string Display, string Normalized)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in vehicles)
        {
            var display = item.VehicleNumber?.Trim();
            if (string.IsNullOrWhiteSpace(display)) continue;
            var normalized = NormalizeVehicleNumber(display);
            if (!seen.Add(normalized))
                throw new InvalidOperationException($"Duplicate vehicle number in request: {display}.");
            normalizedInRequest.Add((display, normalized));
        }

        if (normalizedInRequest.Count == 0) return;

        var apartmentLabel = await Db.Apartments.AsNoTracking()
            .Where(a => a.IdApartment == apartmentId)
            .Select(a => a.ApartmentName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        apartmentLabel = string.IsNullOrWhiteSpace(apartmentLabel) ? "this apartment" : apartmentLabel.Trim();

        var activeInApartment = await (
            from v in Db.Vehicles.AsNoTracking()
            where v.ApartmentId == apartmentId && v.IsActive
            where excludePersonId == null || v.PersonId != excludePersonId.Value
            join p in Db.Persons.AsNoTracking() on v.PersonId equals p.IdPerson
            where p.ApartmentId == apartmentId
            select new { v.VehicleNumber, v.PersonId, p.FullName }
        ).ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (display, normalized) in normalizedInRequest)
        {
            var conflict = activeInApartment.FirstOrDefault(v =>
                NormalizeVehicleNumber(v.VehicleNumber) == normalized);
            if (conflict is null) continue;

            var blockNames = await LoadBlockNamesForPersonAsync(apartmentId, conflict.PersonId, cancellationToken)
                .ConfigureAwait(false);
            var personName = FormatResidentDisplayName(conflict.FullName, conflict.PersonId);
            throw new InvalidOperationException(
                FormatVehicleConflictMessage(display, personName, blockNames, apartmentLabel));
        }
    }

    private async Task<IReadOnlyList<string>> LoadBlockNamesForPersonAsync(
        int apartmentId, int personId, CancellationToken cancellationToken)
    {
        var ownerBlocks = await (
            from uo in Db.UnitOwners.AsNoTracking()
            where uo.ApartmentId == apartmentId && uo.PersonId == personId
            join u in Db.Units.AsNoTracking() on uo.UnitId equals u.IdUnit
            where u.ApartmentId == apartmentId
            join b in Db.Blocks.AsNoTracking() on u.BlockId equals b.IdBlock into blockJoin
            from b in blockJoin.DefaultIfEmpty()
            select ResolveBlockLabel(b != null ? b.BlockName : null, b != null ? b.BlockCode : null, u.Block)
        ).ToListAsync(cancellationToken).ConfigureAwait(false);

        var tenantBlocks = await (
            from ta in Db.TenantAssignments.AsNoTracking()
            where ta.ApartmentId == apartmentId && ta.PersonId == personId
            join u in Db.Units.AsNoTracking() on ta.UnitId equals u.IdUnit
            where u.ApartmentId == apartmentId
            join b in Db.Blocks.AsNoTracking() on u.BlockId equals b.IdBlock into blockJoin
            from b in blockJoin.DefaultIfEmpty()
            select ResolveBlockLabel(b != null ? b.BlockName : null, b != null ? b.BlockCode : null, u.Block)
        ).ToListAsync(cancellationToken).ConfigureAwait(false);

        var currentOwnerBlocks = await (
            from u in Db.Units.AsNoTracking()
            where u.ApartmentId == apartmentId && u.IdCurrentOwner == personId
            join b in Db.Blocks.AsNoTracking() on u.BlockId equals b.IdBlock into blockJoin
            from b in blockJoin.DefaultIfEmpty()
            select ResolveBlockLabel(b != null ? b.BlockName : null, b != null ? b.BlockCode : null, u.Block)
        ).ToListAsync(cancellationToken).ConfigureAwait(false);

        return ownerBlocks
            .Concat(tenantBlocks)
            .Concat(currentOwnerBlocks)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveBlockLabel(string? blockName, string? blockCode, string? legacyBlock) =>
        !string.IsNullOrWhiteSpace(blockName) ? blockName.Trim()
        : !string.IsNullOrWhiteSpace(blockCode) ? blockCode.Trim()
        : !string.IsNullOrWhiteSpace(legacyBlock) ? legacyBlock.Trim()
        : string.Empty;

    private static string FormatResidentDisplayName(string? fullName, int personId) =>
        string.IsNullOrWhiteSpace(fullName) ? $"Person #{personId}" : fullName.Trim();

    private static string FormatVehicleConflictMessage(
        string vehicleNumber,
        string personName,
        IReadOnlyList<string> blockNames,
        string apartmentName)
    {
        if (blockNames.Count == 0)
            return $"Vehicle number '{vehicleNumber}' is already registered to {personName}, apartment {apartmentName}.";
        if (blockNames.Count == 1)
            return $"Vehicle number '{vehicleNumber}' is already registered to {personName}, block {blockNames[0]}, apartment {apartmentName}.";
        return $"Vehicle number '{vehicleNumber}' is already registered to {personName}, blocks {string.Join(", ", blockNames)}, apartment {apartmentName}.";
    }
}
