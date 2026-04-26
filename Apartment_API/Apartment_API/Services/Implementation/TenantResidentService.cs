using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class TenantResidentService(AppDbContext db, IWebHostEnvironment env) : ResidentServiceBase(db, env), ITenantResidentService
{
    public async Task<PagedResult<TenantListDto>> ListAsync(
        int apartmentId, string? search, int? unitId, bool? isActive, int? expiringWithinDays, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var tenantType = await RequirePersonTypeIdAsync(PersonTypeCodes.Tenant, cancellationToken);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = from ta in Db.TenantAssignments.AsNoTracking()
            where ta.ApartmentId == apartmentId
            join p in Db.Persons.AsNoTracking() on ta.PersonId equals p.IdPerson
            where p.PersonTypeId == tenantType
            join u in Db.Units.AsNoTracking() on ta.UnitId equals u.IdUnit
            select new { ta, p, u };
        if (unitId is { } ui) q = q.Where(x => x.ta.UnitId == ui);
        if (isActive == true) q = q.Where(x => x.ta.IsActive);
        if (isActive == false) q = q.Where(x => !x.ta.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search!.Trim();
            q = q.Where(x => x.p.FullName.Contains(s) || x.p.PhoneNumber.Contains(s));
        }
        if (expiringWithinDays is { } d)
        {
            var until = DateTime.UtcNow.Date.AddDays(d);
            q = q.Where(x => x.ta.LeaseEndDate != null && x.ta.LeaseEndDate <= until && x.ta.LeaseEndDate >= DateTime.UtcNow.Date);
        }
        var total = await q.CountAsync(cancellationToken);
        var list = await q.OrderBy(x => x.p.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = new List<TenantListDto>();
        foreach (var x in list)
        {
            var v = await Db.Vehicles.AsNoTracking()
                .Where(ve => ve.ApartmentId == apartmentId && ve.PersonId == x.p.IdPerson && ve.IsActive)
                .ToListAsync(cancellationToken);
            var owner = await (from uo in Db.UnitOwners.AsNoTracking()
                where uo.ApartmentId == apartmentId && uo.UnitId == x.u.IdUnit && uo.IsPrimaryOwner && uo.IsActive
                join op in Db.Persons.AsNoTracking() on uo.PersonId equals op.IdPerson
                select op.FullName).FirstOrDefaultAsync(cancellationToken);
            string? leaseStatus = null;
            if (x.ta.LeaseEndDate is { } le && (le - DateTime.UtcNow.Date).TotalDays is >= 0 and <= 30)
                leaseStatus = "ExpiringSoon";
            items.Add(new TenantListDto
            {
                Id = x.ta.IdTenantAssignment,
                PersonId = x.p.IdPerson,
                FullName = x.p.FullName,
                Email = x.p.Email,
                PhoneNumber = x.p.PhoneNumber,
                UnitId = x.u.IdUnit,
                UnitNumber = x.u.UnitNumber,
                OwnerName = owner,
                LeaseStartDate = x.ta.LeaseStartDate,
                LeaseEndDate = x.ta.LeaseEndDate,
                MonthlyRent = x.ta.MonthlyRent,
                VehicleCount = v.Count,
                VehicleNumbers = v.Select(ve => ve.VehicleNumber).ToList(),
                LeaseStatus = leaseStatus
            });
        }
        return new PagedResult<TenantListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<TenantListDto?> GetByAssignmentIdAsync(
        int apartmentId, int idTenantAssignment, CancellationToken cancellationToken = default)
    {
        var tenantType = await RequirePersonTypeIdAsync(PersonTypeCodes.Tenant, cancellationToken);
        var x = await (from ta in Db.TenantAssignments.AsNoTracking()
            where ta.IdTenantAssignment == idTenantAssignment && ta.ApartmentId == apartmentId
            join p in Db.Persons.AsNoTracking() on ta.PersonId equals p.IdPerson
            where p.PersonTypeId == tenantType
            join u in Db.Units.AsNoTracking() on ta.UnitId equals u.IdUnit
            select new { ta, p, u }).FirstOrDefaultAsync(cancellationToken);
        if (x is null) return null;
        var v = await Db.Vehicles.AsNoTracking()
            .Where(ve => ve.ApartmentId == apartmentId && ve.PersonId == x.p.IdPerson && ve.IsActive)
            .ToListAsync(cancellationToken);
        var owner = await (from uo in Db.UnitOwners.AsNoTracking()
            where uo.ApartmentId == apartmentId && uo.UnitId == x.u.IdUnit && uo.IsPrimaryOwner && uo.IsActive
            join op in Db.Persons.AsNoTracking() on uo.PersonId equals op.IdPerson
            select op.FullName).FirstOrDefaultAsync(cancellationToken);
        string? leaseStatus = null;
        if (x.ta.LeaseEndDate is { } le && (le - DateTime.UtcNow.Date).TotalDays is >= 0 and <= 30)
            leaseStatus = "ExpiringSoon";
        return new TenantListDto
        {
            Id = x.ta.IdTenantAssignment,
            PersonId = x.p.IdPerson,
            FullName = x.p.FullName,
            Email = x.p.Email,
            PhoneNumber = x.p.PhoneNumber,
            UnitId = x.u.IdUnit,
            UnitNumber = x.u.UnitNumber,
            OwnerName = owner,
            LeaseStartDate = x.ta.LeaseStartDate,
            LeaseEndDate = x.ta.LeaseEndDate,
            MonthlyRent = x.ta.MonthlyRent,
            VehicleCount = v.Count,
            VehicleNumbers = v.Select(ve => ve.VehicleNumber).ToList(),
            LeaseStatus = leaseStatus
        };
    }

    public async Task<TenantCreatedDto> CreateAsync(
        int apartmentId, int userId, CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (request.LeaseEndDate is { } le && request.LeaseStartDate > le) throw new InvalidOperationException("Invalid lease range.");
        var active = await Db.TenantAssignments.AnyAsync(
            t => t.ApartmentId == apartmentId && t.UnitId == request.UnitId && t.IsActive && t.VacatedDate == null,
            cancellationToken);
        if (active) throw new InvalidOperationException("Unit already has an active tenant.");
        var tenantType = await RequirePersonTypeIdAsync(PersonTypeCodes.Tenant, cancellationToken);
        var now = DateTime.UtcNow;
        var person = new Person
        {
            ApartmentId = apartmentId,
            PersonNumber = "TEMP",
            PersonTypeId = tenantType,
            FullName = request.FullName.Trim(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber.Trim(),
            IdentityDocTypeId = request.IdentityDocTypeId,
            IdentityDocNumber = request.IdentityDocNumber,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        Db.Persons.Add(person);
        await Db.SaveChangesAsync(cancellationToken);
        person.PersonNumber = $"P-{person.IdPerson:D4}";
        var ta = new TenantAssignment
        {
            ApartmentId = apartmentId,
            UnitId = request.UnitId,
            PersonId = person.IdPerson,
            LeaseStartDate = request.LeaseStartDate.Date,
            LeaseEndDate = request.LeaseEndDate?.Date,
            MonthlyRent = request.MonthlyRent,
            SecurityDeposit = request.SecurityDeposit,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        Db.TenantAssignments.Add(ta);
        foreach (var v in request.Vehicles)
        {
            Db.Vehicles.Add(new Vehicle
            {
                ApartmentId = apartmentId,
                PersonId = person.IdPerson,
                VehicleNumber = v.VehicleNumber,
                Make = v.Make,
                Color = v.Color,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            });
        }
        await Db.SaveChangesAsync(cancellationToken);
        return new TenantCreatedDto { Id = ta.IdTenantAssignment, PersonId = person.IdPerson };
    }

    public async Task UpdateAsync(
        int apartmentId, int userId, int idTenantAssignment, CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (request.LeaseEndDate is { } le2 && request.LeaseStartDate > le2) throw new InvalidOperationException("Invalid lease range.");
        var ta = await Db.TenantAssignments.FirstOrDefaultAsync(
            t => t.IdTenantAssignment == idTenantAssignment && t.ApartmentId == apartmentId, cancellationToken);
        if (ta is null) throw new InvalidOperationException("Tenant assignment not found.");
        var p = await Db.Persons.FirstOrDefaultAsync(
            x => x.IdPerson == ta.PersonId && x.ApartmentId == apartmentId, cancellationToken);
        if (p is null) throw new InvalidOperationException("Person not found.");
        if (ta.UnitId != request.UnitId)
        {
            var taken = await Db.TenantAssignments.AnyAsync(
                t => t.ApartmentId == apartmentId && t.UnitId == request.UnitId && t.IsActive && t.VacatedDate == null
                     && t.IdTenantAssignment != idTenantAssignment,
                cancellationToken);
            if (taken) throw new InvalidOperationException("Unit already has an active tenant.");
        }
        p.FullName = request.FullName.Trim();
        p.Email = request.Email;
        p.PhoneNumber = request.PhoneNumber.Trim();
        p.IdentityDocTypeId = request.IdentityDocTypeId;
        p.IdentityDocNumber = request.IdentityDocNumber;
        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = userId;
        ta.UnitId = request.UnitId;
        ta.LeaseStartDate = request.LeaseStartDate.Date;
        ta.LeaseEndDate = request.LeaseEndDate?.Date;
        ta.MonthlyRent = request.MonthlyRent;
        ta.SecurityDeposit = request.SecurityDeposit;
        ta.UpdatedAt = DateTime.UtcNow;
        ta.UpdatedBy = userId;
        var now = DateTime.UtcNow;
        var existingV = await Db.Vehicles
            .Where(v => v.ApartmentId == apartmentId && v.PersonId == p.IdPerson)
            .ToListAsync(cancellationToken);
        foreach (var ev in existingV)
        {
            ev.IsActive = false;
            ev.UpdatedAt = now;
            ev.UpdatedBy = userId;
        }
        foreach (var vr in request.Vehicles)
        {
            Db.Vehicles.Add(new Vehicle
            {
                ApartmentId = apartmentId,
                PersonId = p.IdPerson,
                VehicleNumber = vr.VehicleNumber.Trim(),
                Make = vr.Make,
                Color = vr.Color,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            });
        }
        await Db.SaveChangesAsync(cancellationToken);
    }

    public async Task VacateAsync(
        int apartmentId, int userId, int idTenantAssignment, VacateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var ta = await Db.TenantAssignments
            .FirstOrDefaultAsync(t => t.IdTenantAssignment == idTenantAssignment && t.ApartmentId == apartmentId, cancellationToken);
        if (ta is null) throw new InvalidOperationException("Not found.");
        ta.VacatedDate = request.VacatedDate.Date;
        ta.VacateRemarks = request.Remarks;
        ta.IsActive = false;
        ta.UpdatedAt = DateTime.UtcNow;
        ta.UpdatedBy = userId;
        var veh = await Db.Vehicles.Where(v => v.ApartmentId == apartmentId && v.PersonId == ta.PersonId).ToListAsync(cancellationToken);
        foreach (var v in veh) v.IsActive = false;
        await Db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantListDto>> GetExpiringAsync(
        int apartmentId, int withinDays, CancellationToken cancellationToken = default) =>
        (await ListAsync(apartmentId, null, null, true, withinDays, 1, 500, cancellationToken)).Items;

    public async Task<IdProofResultDto> UploadLeaseDocumentAsync(
        int apartmentId, int userId, int idTenantAssignment, Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var ta = await Db.TenantAssignments.FirstOrDefaultAsync(
            t => t.IdTenantAssignment == idTenantAssignment && t.ApartmentId == apartmentId, cancellationToken);
        if (ta is null) throw new InvalidOperationException("Tenant assignment not found.");
        var catId = await Db.DocumentCategories.AsNoTracking()
            .Where(c => c.IsActive && c.CategoryCode == "LEASE")
            .Select(c => c.IdDocumentCategory)
            .FirstOrDefaultAsync(cancellationToken);
        if (catId == 0)
            catId = await Db.DocumentCategories.AsNoTracking().Select(c => c.IdDocumentCategory).FirstAsync(cancellationToken);
        var url = UploadFile(fileStream, apartmentId, fileName);
        ta.AgreementDocUrl = url;
        ta.UpdatedAt = DateTime.UtcNow;
        ta.UpdatedBy = userId;
        var doc = new StoredDocument
        {
            ApartmentId = apartmentId,
            CategoryId = catId,
            DocumentName = Path.GetFileName(fileName),
            FileUrl = url,
            LinkedEntityType = "TenantAssignment",
            LinkedEntityId = idTenantAssignment,
            UploadedByUserId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        Db.StoredDocuments.Add(doc);
        await Db.SaveChangesAsync(cancellationToken);
        return new IdProofResultDto { DocumentId = doc.IdDocument, FileUrl = url };
    }
}
