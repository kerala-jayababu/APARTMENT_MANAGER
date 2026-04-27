using System.Security.Cryptography;
using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class OwnerResidentService(
    AppDbContext db,
    IWebHostEnvironment env,
    IPasswordHasher passwordHasher) : ResidentServiceBase(db, env), IOwnerResidentService
{
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    public async Task<PagedResult<OwnerListDto>> ListAsync(
        int apartmentId, string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var ownerTypeId = await RequirePersonTypeIdAsync(PersonTypeCodes.Owner, cancellationToken);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = Db.Persons.AsNoTracking()
            .Where(p => p.ApartmentId == apartmentId && p.PersonTypeId == ownerTypeId);
        if (isActive == true) q = q.Where(p => p.IsActive);
        else if (isActive == false) q = q.Where(p => !p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s2 = search!.Trim();
            var personIdsMatchingUnit = await (
                from uol in Db.UnitOwners.AsNoTracking()
                where uol.ApartmentId == apartmentId && uol.IsActive
                join u in Db.Units.AsNoTracking() on uol.UnitId equals u.IdUnit
                where u.ApartmentId == apartmentId && u.IsActive && u.UnitNumber.Contains(s2)
                join p in Db.Persons.AsNoTracking() on uol.PersonId equals p.IdPerson
                where p.ApartmentId == apartmentId && p.PersonTypeId == ownerTypeId
                select p.IdPerson).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false);
            q = q.Where(p =>
                p.FullName.Contains(s2)
                || (p.PhoneNumber != null && p.PhoneNumber.Contains(s2))
                || (p.Email != null && p.Email.Contains(s2))
                || personIdsMatchingUnit.Contains(p.IdPerson));
        }
        var total = await q.CountAsync(cancellationToken);
        var persons = await q.OrderBy(p => p.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        if (persons.Count == 0) return new PagedResult<OwnerListDto> { Items = [], TotalCount = total, Page = page, PageSize = pageSize };
        var ids = persons.Select(p => p.IdPerson).ToList();
        var idTypes = await Db.IdentityDocTypes.AsNoTracking().ToDictionaryAsync(x => x.IdIdentityDocType, x => x.DocTypeName, cancellationToken);
        var unitOwnerRows = await (from t in Db.UnitOwners.AsNoTracking()
            where t.ApartmentId == apartmentId && t.IsActive && ids.Contains(t.PersonId)
            join u in Db.Units.AsNoTracking() on t.UnitId equals u.IdUnit
            where u.ApartmentId == apartmentId && u.IsActive
            orderby u.UnitNumber
            select new { t.PersonId, u.UnitNumber }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var unitsByP = unitOwnerRows
            .GroupBy(x => x.PersonId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.UnitNumber).Distinct()
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList());
        var veh = await Db.Vehicles.AsNoTracking()
            .Where(v => v.ApartmentId == apartmentId && v.IsActive && ids.Contains(v.PersonId))
            .ToListAsync(cancellationToken);
        var vehG = veh.GroupBy(v => v.PersonId);
        var items = persons.Select(p => new OwnerListDto
        {
            PersonId = p.IdPerson,
            FullName = p.FullName,
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            LinkedUnits = unitsByP.TryGetValue(p.IdPerson, out var lu) ? lu : [],
            IdentityDocType = p.IdentityDocTypeId is { } i && idTypes.TryGetValue(i, out var n) ? n : null,
            IdentityDocNumber = string.IsNullOrWhiteSpace(p.IdentityDocNumber) ? null : p.IdentityDocNumber.Trim(),
            VehicleCount = vehG.FirstOrDefault(g => g.Key == p.IdPerson)?.Count() ?? 0,
            VehicleNumbers = vehG.FirstOrDefault(g => g.Key == p.IdPerson)?.Select(v => v.VehicleNumber).ToList() ?? [],
            IsActive = p.IsActive
        }).ToList();
        return new PagedResult<OwnerListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<OwnerDetailDto?> GetAsync(
        int apartmentId, int personId, CancellationToken cancellationToken = default)
    {
        var p = await Db.Persons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdPerson == personId && x.ApartmentId == apartmentId, cancellationToken);
        if (p is null) return null;
        var links = await (from uo in Db.UnitOwners.AsNoTracking()
            where uo.ApartmentId == apartmentId && uo.PersonId == personId
            join u in Db.Units.AsNoTracking() on uo.UnitId equals u.IdUnit
            select new LinkedUnitItemDto
            {
                UnitOwnerId = uo.IdUnitOwner,
                UnitId = u.IdUnit,
                UnitNumber = u.UnitNumber,
                IsPrimaryOwner = uo.IsPrimaryOwner,
                OwnershipFromDate = uo.OwnershipFromDate
            }).ToListAsync(cancellationToken);
        var vehicles = await Db.Vehicles.AsNoTracking()
            .Where(v => v.ApartmentId == apartmentId && v.PersonId == personId)
            .Select(v => new VehicleItemDto
            {
                Id = v.IdVehicle,
                VehicleNumber = v.VehicleNumber,
                Make = v.Make,
                Color = v.Color
            })
            .ToListAsync(cancellationToken);
        var co = await Db.Persons.AsNoTracking()
            .Where(c => c.ApartmentId == apartmentId && c.ParentOwnerId == personId)
            .Select(c => new CoOwnerMinDto
            {
                PersonId = c.IdPerson, FullName = c.FullName, OwnershipShare = null
            })
            .ToListAsync(cancellationToken);
        return new OwnerDetailDto
        {
            PersonId = p.IdPerson,
            PersonNumber = p.PersonNumber,
            FullName = p.FullName,
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            AlternatePhone = p.AlternatePhone,
            IdentityDocTypeId = p.IdentityDocTypeId,
            IdentityDocNumber = p.IdentityDocNumber,
            PermanentAddress = p.PermanentAddress,
            EmergencyContactName = p.EmergencyContactName,
            EmergencyContactPhone = p.EmergencyContactPhone,
            IsActive = p.IsActive,
            LinkedUnits = links,
            Vehicles = vehicles,
            CoOwners = co
        };
    }

    private const string OwnerRoleCode = "OWNER";

    // ────────────────────────────────────────────────────────────────────
    //  CREATE
    // ────────────────────────────────────────────────────────────────────
    public async Task<int> CreateAsync(
        int apartmentId,
        int userId,
        CreateOwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Sanity ──────────────────────────────────────────────────────
        if (request.LinkedUnitIds is null || request.LinkedUnitIds.Count == 0)
            throw new InvalidOperationException("linkedUnitIds is required.");

        // De-duplicate the unit-id list so we never insert two UnitOwner
        // rows for the same unit.
        var unitIds = request.LinkedUnitIds.Distinct().ToList();

        var phone = request.PhoneNumber.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email!.Trim();

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required so the owner can log into the mobile app.");

        var ownerType = await RequirePersonTypeIdAsync(PersonTypeCodes.Owner, cancellationToken);
        var ownerRoleId = await RequireRoleIdAsync(OwnerRoleCode, cancellationToken);

        // 2. Cross-row validation ────────────────────────────────────────
        var phoneTaken = await Db.Persons.AnyAsync(
            p => p.ApartmentId == apartmentId && p.PhoneNumber == phone && p.IsActive,
            cancellationToken);
        if (phoneTaken)
            throw new InvalidOperationException("Phone number already exists in this apartment.");

        foreach (var uid in unitIds)
        {
            var hasPrimary = await Db.UnitOwners.AnyAsync(
                uo => uo.ApartmentId == apartmentId
                   && uo.UnitId == uid
                   && uo.IsPrimaryOwner
                   && uo.IsActive,
                cancellationToken);
            if (hasPrimary)
                throw new InvalidOperationException($"Unit {uid} already has a primary owner.");
        }

        // 3. Persist within a single transaction ─────────────────────────
        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        // 3a. Create the User row first — needed for Person.LinkedUserId
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = phone,
            PasswordHash = GenerateOtpOnlyPasswordHash(), // forced password reset on first OTP login
            IsSuperAdmin = false,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync(cancellationToken);

        // 3b. Tenant-scope mapping → ApartmentUsers
        Db.ApartmentUsers.Add(new ApartmentUser
        {
            ApartmentId = apartmentId,
            UserId = user.IdUser,
            RoleId = ownerRoleId,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        });

        // 3c. Person row (with LinkedUserId)
        var person = new Person
        {
            ApartmentId = apartmentId,
            PersonNumber = "TEMP",                  // overwritten after we know IdPerson
            PersonTypeId = ownerType,
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = phone,
            AlternatePhone = request.AlternatePhone,
            IdentityDocTypeId = request.IdentityDocTypeId,
            IdentityDocNumber = request.IdentityDocNumber,
            PermanentAddress = request.PermanentAddress,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            LinkedUserId = user.IdUser,             // ← NEW: wires the resident profile to the login user
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        Db.Persons.Add(person);
        await Db.SaveChangesAsync(cancellationToken);

        person.PersonNumber = $"P-{person.IdPerson:D4}";

        // 3d. UnitOwners — first deduped unit becomes primary
        var isFirst = true;
        foreach (var uid in unitIds)
        {
            Db.UnitOwners.Add(new UnitOwner
            {
                ApartmentId = apartmentId,
                UnitId = uid,
                PersonId = person.IdPerson,
                IsPrimaryOwner = isFirst,
                OwnershipFromDate = request.OwnershipFromDate,   // assumes DateOnly DTO
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            });
            isFirst = false;
        }

        // 3e. Vehicles — null-safe, skip blank numbers
        if (request.Vehicles is { Count: > 0 } vehicles)
        {
            foreach (var v in vehicles)
            {
                var vno = v.VehicleNumber?.Trim();
                if (string.IsNullOrWhiteSpace(vno))
                    continue;

                Db.Vehicles.Add(new Vehicle
                {
                    ApartmentId = apartmentId,
                    PersonId = person.IdPerson,
                    VehicleNumber = vno,
                    Make = string.IsNullOrWhiteSpace(v.Make) ? null : v.Make!.Trim(),
                    Color = string.IsNullOrWhiteSpace(v.Color) ? null : v.Color!.Trim(),
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = userId
                });
            }
        }

        await Db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return person.IdPerson;
    }

    // ────────────────────────────────────────────────────────────────────
    //  UPDATE
    // ────────────────────────────────────────────────────────────────────
    public async Task UpdateAsync(
        int apartmentId,
        int userId,
        int personId,
        CreateOwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        var p = await Db.Persons.FirstOrDefaultAsync(
            x => x.IdPerson == personId && x.ApartmentId == apartmentId,
            cancellationToken);
        if (p is null) throw new InvalidOperationException("Owner not found.");

        var newPhone = request.PhoneNumber.Trim();
        var newEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email!.Trim();

        // 1. Phone-change uniqueness check ───────────────────────────────
        if (!string.Equals(p.PhoneNumber, newPhone, StringComparison.Ordinal))
        {
            var phoneTaken = await Db.Persons.AnyAsync(
                x => x.ApartmentId == apartmentId
                  && x.IdPerson != personId
                  && x.PhoneNumber == newPhone
                  && x.IsActive,
                cancellationToken);
            if (phoneTaken)
                throw new InvalidOperationException("Phone number already exists in this apartment.");
        }

        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        // 2. Person scalar fields ────────────────────────────────────────
        p.FullName = request.FullName.Trim();
        p.Email = newEmail;
        p.PhoneNumber = newPhone;
        p.AlternatePhone = request.AlternatePhone;
        p.IdentityDocTypeId = request.IdentityDocTypeId;
        p.IdentityDocNumber = request.IdentityDocNumber;
        p.PermanentAddress = request.PermanentAddress;
        p.EmergencyContactName = request.EmergencyContactName;
        p.EmergencyContactPhone = request.EmergencyContactPhone;
        p.UpdatedAt = now;
        p.UpdatedBy = userId;

        // 2a. Mirror name / email / phone onto the linked Users row
        if (p.LinkedUserId is int luid)
        {
            var u = await Db.Users.FirstOrDefaultAsync(
                x => x.IdUser == luid, cancellationToken);
            if (u is not null)
            {
                u.FullName = p.FullName;
                if (newEmail is not null) u.Email = newEmail;
                u.PhoneNumber = p.PhoneNumber;
                u.UpdatedAt = now;
                u.UpdatedBy = userId;
            }
        }

        // 3. UnitOwners diff ─────────────────────────────────────────────
        if (request.LinkedUnitIds is { Count: > 0 } rawUids)
        {
            var uids = rawUids.Distinct().ToHashSet();

            var existing = await Db.UnitOwners
                .Where(uo => uo.ApartmentId == apartmentId
                          && uo.PersonId == personId
                          && uo.IsActive)
                .ToListAsync(cancellationToken);

            // Soft-delete units the caller dropped
            foreach (var e in existing.Where(e => !uids.Contains(e.UnitId)))
            {
                e.IsActive = false;
                e.UpdatedAt = now;
                e.UpdatedBy = userId;
            }

            // The "post-deactivation" view — used to know if a primary still exists
            var stillActiveAfterDrop = existing
                .Where(e => uids.Contains(e.UnitId))
                .ToList();
            var hasPrimaryAfterDrop = stillActiveAfterDrop.Any(e => e.IsPrimaryOwner);

            // Pre-check: any newly-added unit must not already have a primary owner
            var have = existing.Select(x => x.UnitId).ToHashSet();
            var toAdd = uids.Where(uid => !have.Contains(uid)).ToList();

            foreach (var uid in toAdd)
            {
                var taken = await Db.UnitOwners.AnyAsync(
                    uo => uo.ApartmentId == apartmentId
                       && uo.UnitId == uid
                       && uo.PersonId != personId
                       && uo.IsPrimaryOwner
                       && uo.IsActive,
                    cancellationToken);
                if (taken)
                    throw new InvalidOperationException($"Unit {uid} already has a primary owner.");
            }

            // Insert new rows; the very first one becomes primary if no
            // primary survives the soft-delete step above.
            var assignedFirst = hasPrimaryAfterDrop;
            foreach (var uid in toAdd)
            {
                Db.UnitOwners.Add(new UnitOwner
                {
                    ApartmentId = apartmentId,
                    UnitId = uid,
                    PersonId = personId,
                    IsPrimaryOwner = !assignedFirst,
                    OwnershipFromDate = request.OwnershipFromDate,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = userId
                });
                assignedFirst = true;
            }
        }

        // 4. Vehicles diff ───────────────────────────────────────────────
        await ReconcileVehiclesAsync(apartmentId, userId, personId, request.Vehicles, now, cancellationToken);

        await Db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
    public async Task<IdProofResultDto> UploadIdProofAsync(
        int apartmentId, int userId, int personId, Stream fileStream, string fileName, string? documentCategoryCode,
        CancellationToken cancellationToken = default)
    {
        var p = await Db.Persons.FirstOrDefaultAsync(x => x.IdPerson == personId && x.ApartmentId == apartmentId, cancellationToken);
        if (p is null) throw new InvalidOperationException("Person not found.");
        var catId = await Db.DocumentCategories.AsNoTracking()
            .Where(c => c.IsActive && (documentCategoryCode == null || c.CategoryCode == documentCategoryCode))
            .Select(c => c.IdDocumentCategory)
            .FirstOrDefaultAsync(cancellationToken);
        if (catId == 0) catId = await Db.DocumentCategories.AsNoTracking().Select(c => c.IdDocumentCategory).FirstAsync(cancellationToken);
        var url = UploadFile(fileStream, apartmentId, fileName);
        var doc = new StoredDocument
        {
            ApartmentId = apartmentId,
            CategoryId = catId,
            DocumentName = Path.GetFileName(fileName),
            FileUrl = url,
            LinkedEntityType = "Person",
            LinkedEntityId = personId,
            UploadedByUserId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        Db.StoredDocuments.Add(doc);
        await Db.SaveChangesAsync(cancellationToken);
        return new IdProofResultDto { DocumentId = doc.IdDocument, FileUrl = url };
    }

    private async Task<int> RequireRoleIdAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        var code = roleCode.Trim();
        var id = await Db.AppRoles.AsNoTracking()
            .Where(r => r.IsActive && r.RoleCode == code)
            .Select(r => r.IdRole)
            .FirstOrDefaultAsync(cancellationToken);
        if (id == 0)
        {
            var roles = await Db.AppRoles.AsNoTracking()
                .Where(r => r.IsActive)
                .ToListAsync(cancellationToken);
            var match = roles.FirstOrDefault(r =>
                string.Equals(r.RoleCode, code, StringComparison.OrdinalIgnoreCase));
            if (match is not null) id = match.IdRole;
        }
        if (id == 0)
            throw new InvalidOperationException($"App role not found: {code}.");
        return id;
    }

    private string GenerateOtpOnlyPasswordHash() =>
        _passwordHasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));

    private async Task ReconcileVehiclesAsync(
        int apartmentId,
        int userId,
        int personId,
        List<VehicleRequestItem>? vehicles,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var existing = await Db.Vehicles
            .Where(ve => ve.ApartmentId == apartmentId && ve.PersonId == personId)
            .ToListAsync(cancellationToken);
        foreach (var ev in existing)
        {
            ev.IsActive = false;
            ev.UpdatedAt = now;
            ev.UpdatedBy = userId;
        }
        if (vehicles is not { Count: > 0 }) return;
        foreach (var vr in vehicles)
        {
            var vno = vr.VehicleNumber?.Trim();
            if (string.IsNullOrWhiteSpace(vno)) continue;
            Db.Vehicles.Add(new Vehicle
            {
                ApartmentId = apartmentId,
                PersonId = personId,
                VehicleNumber = vno,
                Make = string.IsNullOrWhiteSpace(vr.Make) ? null : vr.Make!.Trim(),
                Color = string.IsNullOrWhiteSpace(vr.Color) ? null : vr.Color!.Trim(),
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            });
        }
    }
}
