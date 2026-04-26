using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class CoOwnerResidentService(
    AppDbContext db,
    IWebHostEnvironment env,
    IOwnerResidentService owners) : ResidentServiceBase(db, env), ICoOwnerResidentService
{
    public async Task<PagedResult<CoOwnerListDto>> ListAsync(
        int apartmentId, int? primaryOwnerPersonId, int? unitId, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = from uo in Db.UnitOwners.AsNoTracking()
            where uo.ApartmentId == apartmentId && uo.IsActive && !uo.IsPrimaryOwner
            join p in Db.Persons.AsNoTracking() on uo.PersonId equals p.IdPerson
            where p.ParentOwnerId != null
            join u in Db.Units.AsNoTracking() on uo.UnitId equals u.IdUnit
            join pr in Db.Persons.AsNoTracking() on p.ParentOwnerId equals pr.IdPerson
            select new { uo, p, u, pr };
        if (primaryOwnerPersonId is { } po) q = q.Where(x => x.p.ParentOwnerId == po);
        if (unitId is { } ui) q = q.Where(x => x.uo.UnitId == ui);
        if (isActive == true) q = q.Where(x => x.p.IsActive);
        else if (isActive == false) q = q.Where(x => !x.p.IsActive);
        var total = await q.CountAsync(cancellationToken);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var list = await q.OrderBy(x => x.p.FullName).ThenBy(x => x.u.UnitNumber)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = list.Select(x => new CoOwnerListDto
        {
            Id = x.uo.IdUnitOwner,
            PrimaryOwnerPersonId = x.p.ParentOwnerId ?? 0,
            PrimaryOwnerName = x.pr.FullName,
            UnitId = x.uo.UnitId,
            UnitNumber = x.u.UnitNumber,
            CoOwnerPersonId = x.p.IdPerson,
            CoOwnerName = x.p.FullName,
            PhoneNumber = x.p.PhoneNumber,
            OwnershipSharePct = x.uo.OwnershipSharePct,
            IsActive = x.p.IsActive
        }).ToList();
        return new PagedResult<CoOwnerListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<OwnerDetailDto?> GetByUnitOwnerIdAsync(
        int apartmentId, int idUnitOwner, CancellationToken cancellationToken = default)
    {
        var pid = await Db.UnitOwners.AsNoTracking()
            .Where(uo => uo.IdUnitOwner == idUnitOwner && uo.ApartmentId == apartmentId)
            .Select(uo => uo.PersonId)
            .FirstOrDefaultAsync(cancellationToken);
        if (pid == 0) return null;
        return await owners.GetAsync(apartmentId, pid, cancellationToken);
    }

    public async Task<CoOwnerCreatedDto> CreateAsync(
        int apartmentId, int userId, CreateCoOwnerRequest request, CancellationToken cancellationToken = default)
    {
        var coType = await RequirePersonTypeIdAsync(PersonTypeCodes.CoOwner, cancellationToken);
        var coCount = await Db.Persons.CountAsync(
            p => p.ApartmentId == apartmentId && p.ParentOwnerId == request.PrimaryOwnerPersonId, cancellationToken);
        if (coCount >= 3) throw new InvalidOperationException("Maximum 3 co-owners per primary owner context.");
        var now = DateTime.UtcNow;
        var person = new Person
        {
            ApartmentId = apartmentId,
            PersonNumber = "TEMP",
            PersonTypeId = coType,
            ParentOwnerId = request.PrimaryOwnerPersonId,
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
        var uo = new UnitOwner
        {
            ApartmentId = apartmentId,
            UnitId = request.UnitId,
            PersonId = person.IdPerson,
            IsPrimaryOwner = false,
            OwnershipFromDate = now.Date,
            IsActive = true,
            OwnershipSharePct = request.OwnershipSharePct,
            CreatedAt = now,
            CreatedBy = userId
        };
        Db.UnitOwners.Add(uo);
        await Db.SaveChangesAsync(cancellationToken);
        return new CoOwnerCreatedDto { Id = uo.IdUnitOwner, PersonId = person.IdPerson };
    }

    public async Task UpdateAsync(
        int apartmentId, int userId, int idUnitOwner, CreateCoOwnerRequest request, CancellationToken cancellationToken = default)
    {
        var uo = await Db.UnitOwners.FirstOrDefaultAsync(
            x => x.IdUnitOwner == idUnitOwner && x.ApartmentId == apartmentId, cancellationToken);
        if (uo is null) throw new InvalidOperationException("Co-owner not found.");
        var p = await Db.Persons.FirstOrDefaultAsync(
            x => x.IdPerson == uo.PersonId && x.ApartmentId == apartmentId, cancellationToken);
        if (p is null) throw new InvalidOperationException("Person not found.");
        p.ParentOwnerId = request.PrimaryOwnerPersonId;
        p.FullName = request.FullName.Trim();
        p.PhoneNumber = request.PhoneNumber.Trim();
        p.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email!.Trim();
        p.IdentityDocTypeId = request.IdentityDocTypeId;
        p.IdentityDocNumber = request.IdentityDocNumber;
        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = userId;
        uo.UnitId = request.UnitId;
        uo.OwnershipSharePct = request.OwnershipSharePct;
        uo.UpdatedAt = DateTime.UtcNow;
        uo.UpdatedBy = userId;
        await Db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int apartmentId, int idUnitOwner, int userId, CancellationToken cancellationToken = default)
    {
        var uo = await Db.UnitOwners.FirstOrDefaultAsync(
            x => x.IdUnitOwner == idUnitOwner && x.ApartmentId == apartmentId, cancellationToken);
        if (uo is null) return;
        uo.IsActive = false;
        uo.UpdatedAt = DateTime.UtcNow;
        uo.UpdatedBy = userId;
        await Db.SaveChangesAsync(cancellationToken);
    }
}
