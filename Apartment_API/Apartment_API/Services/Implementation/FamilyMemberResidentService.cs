using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class FamilyMemberResidentService(AppDbContext db, IWebHostEnvironment env) : ResidentServiceBase(db, env), IFamilyMemberResidentService
{
    public async Task<PagedResult<FamilyMemberDto>> ListAsync(
        int apartmentId, int? unitId, int? parentPersonId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var fam = await RequirePersonTypeIdAsync(PersonTypeCodes.FamilyMember, cancellationToken);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = Db.Persons.AsNoTracking()
            .Where(p => p.ApartmentId == apartmentId && p.PersonTypeId == fam);
        if (parentPersonId is { } pp) q = q.Where(p => p.ParentOwnerId == pp);
        if (unitId is { } ui)
        {
            var pids = await Db.UnitOwners.AsNoTracking()
                .Where(x => x.ApartmentId == apartmentId && x.UnitId == ui && x.IsActive)
                .Select(x => x.PersonId)
                .ToListAsync(cancellationToken);
            q = q.Where(p => p.ParentOwnerId != null && pids.Contains(p.ParentOwnerId.Value));
        }
        var total = await q.CountAsync(cancellationToken);
        var list = await q.OrderBy(p => p.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = new List<FamilyMemberDto>();
        foreach (var p in list)
        {
            var uo = await Db.UnitOwners.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.PersonId == (p.ParentOwnerId ?? 0) && x.IsActive, cancellationToken);
            var un = uo is null
                ? ""
                : (await Db.Units.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IdUnit == uo.UnitId, cancellationToken))?.UnitNumber ?? "";
            items.Add(new FamilyMemberDto
            {
                PersonId = p.IdPerson,
                UnitId = uo?.UnitId ?? 0,
                UnitNumber = un,
                FullName = p.FullName,
                Relationship = p.Relationship,
                Age = p.Age,
                Gender = p.Gender,
                ContactNumber = p.PhoneNumber,
                SpecialNotes = p.SpecialNotes
            });
        }
        return new PagedResult<FamilyMemberDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<FamilyMemberDto?> GetFamilyMemberAsync(
        int apartmentId, int personId, CancellationToken cancellationToken = default)
    {
        var fam = await RequirePersonTypeIdAsync(PersonTypeCodes.FamilyMember, cancellationToken);
        var p = await Db.Persons.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IdPerson == personId && x.ApartmentId == apartmentId && x.PersonTypeId == fam, cancellationToken);
        if (p is null) return null;
        var uo = await Db.UnitOwners.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ApartmentId == apartmentId && x.PersonId == (p.ParentOwnerId ?? 0) && x.IsActive, cancellationToken);
        var un = uo is null
            ? ""
            : (await Db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.IdUnit == uo.UnitId, cancellationToken))?.UnitNumber ?? "";
        return new FamilyMemberDto
        {
            PersonId = p.IdPerson,
            UnitId = uo?.UnitId ?? 0,
            UnitNumber = un,
            FullName = p.FullName,
            Relationship = p.Relationship,
            Age = p.Age,
            Gender = p.Gender,
            ContactNumber = p.PhoneNumber,
            SpecialNotes = p.SpecialNotes
        };
    }

    public async Task<int> CreateAsync(
        int apartmentId, int userId, CreateFamilyMemberRequest request, CancellationToken cancellationToken = default)
    {
        var fam = await RequirePersonTypeIdAsync(PersonTypeCodes.FamilyMember, cancellationToken);
        var now = DateTime.UtcNow;
        var p = new Person
        {
            ApartmentId = apartmentId,
            PersonNumber = "TEMP",
            PersonTypeId = fam,
            ParentOwnerId = request.ParentPersonId,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.ContactNumber ?? "",
            Relationship = request.Relationship,
            Age = request.Age,
            Gender = request.Gender,
            SpecialNotes = request.SpecialNotes,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        Db.Persons.Add(p);
        await Db.SaveChangesAsync(cancellationToken);
        p.PersonNumber = $"P-{p.IdPerson:D4}";
        await Db.SaveChangesAsync(cancellationToken);
        return p.IdPerson;
    }

    public async Task UpdateAsync(
        int apartmentId, int userId, int personId, CreateFamilyMemberRequest request, CancellationToken cancellationToken = default)
    {
        var p = await Db.Persons.FirstOrDefaultAsync(x => x.IdPerson == personId && x.ApartmentId == apartmentId, cancellationToken);
        if (p is null) return;
        p.FullName = request.FullName.Trim();
        p.PhoneNumber = request.ContactNumber ?? p.PhoneNumber;
        p.Relationship = request.Relationship;
        p.Age = request.Age;
        p.Gender = request.Gender;
        p.SpecialNotes = request.SpecialNotes;
        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = userId;
        await Db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        int apartmentId, int personId, CancellationToken cancellationToken = default) =>
        await Db.Persons.Where(p => p.IdPerson == personId && p.ApartmentId == apartmentId)
            .ExecuteDeleteAsync(cancellationToken);
}
