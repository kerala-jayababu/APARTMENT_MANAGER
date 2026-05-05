using Apartment_API.Data;
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
}
