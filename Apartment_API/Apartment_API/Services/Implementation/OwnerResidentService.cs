using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class OwnerResidentService(AppDbContext db, IWebHostEnvironment env) : ResidentServiceBase(db, env), IOwnerResidentService
{
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
            q = q.Where(p => p.FullName.Contains(s2) || (p.PhoneNumber != null && p.PhoneNumber.Contains(s2)) || (p.Email != null && p.Email.Contains(s2)));
        }
        var total = await q.CountAsync(cancellationToken);
        var persons = await q.OrderBy(p => p.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        if (persons.Count == 0) return new PagedResult<OwnerListDto> { Items = [], TotalCount = total, Page = page, PageSize = pageSize };
        var ids = persons.Select(p => p.IdPerson).ToList();
        var idTypes = await Db.IdentityDocTypes.AsNoTracking().ToDictionaryAsync(x => x.IdIdentityDocType, x => x.DocTypeName, cancellationToken);
        var uo = await (from t in Db.UnitOwners.AsNoTracking()
            where t.ApartmentId == apartmentId && t.IsActive && t.IsPrimaryOwner && ids.Contains(t.PersonId)
            join u in Db.Units.AsNoTracking() on t.UnitId equals u.IdUnit
            select new { t.PersonId, u.UnitNumber }).ToListAsync(cancellationToken);
        var unitsByP = uo.GroupBy(x => x.PersonId).ToDictionary(g => g.Key, g => g.Select(x => x.UnitNumber).ToList());
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

    public async Task<int> CreateAsync(
        int apartmentId, int userId, CreateOwnerRequest request, CancellationToken cancellationToken = default)
    {
        if (request.LinkedUnitIds is null || request.LinkedUnitIds.Count == 0)
            throw new InvalidOperationException("linkedUnitIds is required.");
        var ownerType = await RequirePersonTypeIdAsync(PersonTypeCodes.Owner, cancellationToken);
        var dup = await Db.Persons.AnyAsync(
            p => p.ApartmentId == apartmentId && p.PhoneNumber == request.PhoneNumber.Trim() && p.IsActive,
            cancellationToken);
        if (dup) throw new InvalidOperationException("Phone number already exists in this apartment.");
        foreach (var uid in request.LinkedUnitIds)
        {
            var hasPrimary = await Db.UnitOwners.AnyAsync(
                uo => uo.ApartmentId == apartmentId && uo.UnitId == uid && uo.IsPrimaryOwner && uo.IsActive,
                cancellationToken);
            if (hasPrimary) throw new InvalidOperationException($"Unit {uid} already has a primary owner.");
        }
        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var person = new Person
        {
            ApartmentId = apartmentId,
            PersonNumber = "TEMP",
            PersonTypeId = ownerType,
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email!.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            AlternatePhone = request.AlternatePhone,
            IdentityDocTypeId = request.IdentityDocTypeId,
            IdentityDocNumber = request.IdentityDocNumber,
            PermanentAddress = request.PermanentAddress,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        Db.Persons.Add(person);
        await Db.SaveChangesAsync(cancellationToken);
        person.PersonNumber = $"P-{person.IdPerson:D4}";
        await Db.SaveChangesAsync(cancellationToken);
        var isFirst = true;
        foreach (var uid in request.LinkedUnitIds)
        {
            Db.UnitOwners.Add(new UnitOwner
            {
                ApartmentId = apartmentId,
                UnitId = uid,
                PersonId = person.IdPerson,
                IsPrimaryOwner = isFirst,
                OwnershipFromDate = request.OwnershipFromDate.Date,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            });
            isFirst = false;
        }
        foreach (var v in request.Vehicles)
        {
            Db.Vehicles.Add(new Vehicle
            {
                ApartmentId = apartmentId,
                PersonId = person.IdPerson,
                VehicleNumber = v.VehicleNumber.Trim(),
                Make = v.Make,
                Color = v.Color,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            });
        }
        await Db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return person.IdPerson;
    }

    public async Task UpdateAsync(
        int apartmentId, int userId, int personId, CreateOwnerRequest request, CancellationToken cancellationToken = default)
    {
        var p = await Db.Persons.FirstOrDefaultAsync(x => x.IdPerson == personId && x.ApartmentId == apartmentId, cancellationToken);
        if (p is null) throw new InvalidOperationException("Owner not found.");
        p.FullName = request.FullName.Trim();
        p.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email!.Trim();
        p.PhoneNumber = request.PhoneNumber.Trim();
        p.AlternatePhone = request.AlternatePhone;
        p.IdentityDocTypeId = request.IdentityDocTypeId;
        p.IdentityDocNumber = request.IdentityDocNumber;
        p.PermanentAddress = request.PermanentAddress;
        p.EmergencyContactName = request.EmergencyContactName;
        p.EmergencyContactPhone = request.EmergencyContactPhone;
        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = userId;
        if (request.LinkedUnitIds is { Count: > 0 } uids)
        {
            var existing = await Db.UnitOwners
                .Where(uo => uo.ApartmentId == apartmentId && uo.PersonId == personId && uo.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var e in existing.Where(e => !uids.Contains(e.UnitId)))
            {
                e.IsActive = false;
                e.UpdatedAt = DateTime.UtcNow;
                e.UpdatedBy = userId;
            }
            var have = existing.Select(x => x.UnitId).ToHashSet();
            var now = DateTime.UtcNow;
            foreach (var uid in uids.Where(uid => !have.Contains(uid)))
            {
                Db.UnitOwners.Add(new UnitOwner
                {
                    ApartmentId = apartmentId,
                    UnitId = uid,
                    PersonId = personId,
                    IsPrimaryOwner = !existing.Any(x => x.IsPrimaryOwner),
                    OwnershipFromDate = request.OwnershipFromDate.Date,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = userId
                });
            }
        }
        await Db.SaveChangesAsync(cancellationToken);
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
}
