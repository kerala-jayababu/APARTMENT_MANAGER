using System.Security.Cryptography;
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
    IPasswordHasher passwordHasher,
    IOwnerResidentService owners) : ResidentServiceBase(db, env), ICoOwnerResidentService
{
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private const string DefaultCoOwnerApartmentRoleCode = "RESIDENT";
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
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (request.CreateAppLogin && string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required when createAppLogin is true.");

        var coType = await RequirePersonTypeIdAsync(PersonTypeCodes.CoOwner, cancellationToken);
        var coCount = await Db.Persons.CountAsync(
            p => p.ApartmentId == apartmentId && p.ParentOwnerId == request.PrimaryOwnerPersonId, cancellationToken);
        if (coCount >= 3) throw new InvalidOperationException("Maximum 3 co-owners per primary owner context.");
        var apartmentRoleCode = string.IsNullOrWhiteSpace(request.ApartmentAccessRoleCode)
            ? DefaultCoOwnerApartmentRoleCode
            : request.ApartmentAccessRoleCode.Trim();
        var apartmentRoleId = request.CreateAppLogin
            ? await RequireRoleIdAsync(apartmentRoleCode, cancellationToken)
            : 0;

        var phone = request.PhoneNumber.Trim();

        var unitExists = await Db.Units.AnyAsync(
            u => u.IdUnit == request.UnitId && u.ApartmentId == apartmentId && u.IsActive,
            cancellationToken);
        if (!unitExists)
            throw new InvalidOperationException(
                $"unitId {request.UnitId} does not exist in this apartment.");

        var primaryOwnerExists = await Db.Persons.AnyAsync(
            p => p.IdPerson == request.PrimaryOwnerPersonId && p.ApartmentId == apartmentId && p.IsActive,
            cancellationToken);
        if (!primaryOwnerExists)
            throw new InvalidOperationException(
                $"primaryOwnerPersonId {request.PrimaryOwnerPersonId} does not exist in this apartment.");

        var phoneTaken = await Db.Persons.AnyAsync(
            p => p.ApartmentId == apartmentId && p.PhoneNumber == phone && p.IsActive,
            cancellationToken);
        if (phoneTaken)
            throw new InvalidOperationException("Phone number already exists in this apartment.");

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailTakenInPersons = await Db.Persons.AnyAsync(
                p => p.ApartmentId == apartmentId && p.Email == email && p.IsActive,
                cancellationToken);
            if (emailTakenInPersons)
                throw new InvalidOperationException("Email already exists in this apartment.");
        }

        if (request.CreateAppLogin)
        {
            var emailTakenInUsers = await Db.Users.AnyAsync(
                u => u.Email == email, cancellationToken);
            if (emailTakenInUsers)
                throw new InvalidOperationException("A login already exists for this email. Please use a different email.");

            var phoneTakenInUsers = await Db.Users.AnyAsync(
                u => u.PhoneNumber == phone && u.IsActive, cancellationToken);
            if (phoneTakenInUsers)
                throw new InvalidOperationException("A login already exists for this phone number. Please use a different phone number.");
        }

        await using var tx = await Db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var fromDate = (request.OwnershipFromDate ?? now).Date;

        var person = new Person
        {
            ApartmentId = apartmentId,
            PersonNumber = "TEMP",
            PersonTypeId = coType,
            ParentOwnerId = request.PrimaryOwnerPersonId,
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = phone,
            IdentityDocTypeId = request.IdentityDocTypeId,
            IdentityDocNumber = request.IdentityDocNumber,
            LinkedUserId = null,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };
        Db.Persons.Add(person);
        await Db.SaveChangesAsync(cancellationToken);
        person.PersonNumber = $"P-{person.IdPerson:D4}";

        if (request.CreateAppLogin)
        {
            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email!,
                PhoneNumber = phone,
                PasswordHash = _passwordHasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))),
                IsSuperAdmin = false,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            };
            Db.Users.Add(user);
            await Db.SaveChangesAsync(cancellationToken);
            Db.ApartmentUsers.Add(new ApartmentUser
            {
                ApartmentId = apartmentId,
                UserId = user.IdUser,
                RoleId = apartmentRoleId,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            });
            person.LinkedUserId = user.IdUser;
            await Db.SaveChangesAsync(cancellationToken);
        }

        var uo = new UnitOwner
        {
            ApartmentId = apartmentId,
            UnitId = request.UnitId,
            PersonId = person.IdPerson,
            IsPrimaryOwner = false,
            OwnershipFromDate = fromDate,
            IsActive = true,
            OwnershipSharePct = request.OwnershipSharePct,
            CreatedAt = now,
            CreatedBy = userId
        };
        Db.UnitOwners.Add(uo);
        await Db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
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
