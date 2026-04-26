using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation.Committee;

public sealed class CommitteeMemberService(AppDbContext db, CommitteeDataHelper helper) : ICommitteeMemberService
{
    public async Task<IReadOnlyList<CommitteeMemberListDto>> GetMembersForTenureAsync(
        int apartmentId, int tenureId, CancellationToken cancellationToken = default) =>
        (await ListMembersInternalAsync(apartmentId, tenureId, null, null, null, 1, 500, cancellationToken)).Items;

    public async Task<PagedResult<CommitteeMemberListDto>> ListMembersAsync(
        int apartmentId, int? committeeTenureId, int? committeeRoleId, string? statusCode, string? search, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        await ListMembersInternalAsync(
            apartmentId, committeeTenureId, committeeRoleId, statusCode, search, page, pageSize, cancellationToken);

    private async Task<PagedResult<CommitteeMemberListDto>> ListMembersInternalAsync(
        int apartmentId, int? committeeTenureId, int? committeeRoleId, string? statusCode, string? search, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = from m in db.CommitteeMembers.AsNoTracking()
            where m.ApartmentId == apartmentId
            join ct in db.CommitteeTenures.AsNoTracking() on m.CommitteeTenureId equals ct.IdCommitteeTenure
            join r in db.CommitteeRoles.AsNoTracking() on m.CommitteeRoleId equals r.IdCommitteeRole
            join s in db.CommitteeMemberStatuses.AsNoTracking() on m.StatusId equals s.IdCommitteeMemberStatus
            join p in db.Persons.AsNoTracking() on m.PersonId equals p.IdPerson
            select new { m, ct, r, s, p };
        if (committeeTenureId is { } tid) q = q.Where(x => x.m.CommitteeTenureId == tid);
        if (committeeRoleId is { } rid) q = q.Where(x => x.m.CommitteeRoleId == rid);
        if (!string.IsNullOrWhiteSpace(statusCode)) q = q.Where(x => x.s.StatusCode == statusCode);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s0 = search.Trim();
            q = q.Where(x => x.p.FullName.Contains(s0));
        }
        var total = await q.CountAsync(cancellationToken);
        var list = await q
            .OrderByDescending(x => x.m.EffectiveFromDate)
            .ThenBy(x => x.p.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = new List<CommitteeMemberListDto>();
        foreach (var x in list)
        {
            var un = await helper.GetPrimaryUnitNumberAsync(apartmentId, x.p.IdPerson, cancellationToken);
            items.Add(new CommitteeMemberListDto
            {
                Id = x.m.IdCommitteeMember,
                CommitteeTenureId = x.m.CommitteeTenureId,
                TenureName = Committee.FormatTenureName(x.ct),
                PersonId = x.p.IdPerson,
                FullName = x.p.FullName,
                UnitNumber = un,
                PhoneNumber = x.p.PhoneNumber,
                Email = x.p.Email,
                CommitteeRoleId = x.r.IdCommitteeRole,
                CommitteeRoleCode = x.r.RoleCode,
                CommitteeRoleName = x.r.RoleName,
                EffectiveFromDate = x.m.EffectiveFromDate,
                EffectiveToDate = x.m.EffectiveToDate,
                StatusCode = x.s.StatusCode,
                StatusName = x.s.StatusName
            });
        }
        return new PagedResult<CommitteeMemberListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<CommitteeMemberListDto?> GetMemberByIdAsync(
        int apartmentId, int id, CancellationToken cancellationToken = default)
    {
        var row = await (from m in db.CommitteeMembers.AsNoTracking()
            where m.IdCommitteeMember == id && m.ApartmentId == apartmentId
            join ct in db.CommitteeTenures.AsNoTracking() on m.CommitteeTenureId equals ct.IdCommitteeTenure
            join r in db.CommitteeRoles.AsNoTracking() on m.CommitteeRoleId equals r.IdCommitteeRole
            join s in db.CommitteeMemberStatuses.AsNoTracking() on m.StatusId equals s.IdCommitteeMemberStatus
            join p in db.Persons.AsNoTracking() on m.PersonId equals p.IdPerson
            select new { m, ct, r, s, p }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;
        var un = await helper.GetPrimaryUnitNumberAsync(apartmentId, row.p.IdPerson, cancellationToken);
        return new CommitteeMemberListDto
        {
            Id = row.m.IdCommitteeMember,
            CommitteeTenureId = row.m.CommitteeTenureId,
            TenureName = Committee.FormatTenureName(row.ct),
            PersonId = row.p.IdPerson,
            FullName = row.p.FullName,
            UnitNumber = un,
            PhoneNumber = row.p.PhoneNumber,
            Email = row.p.Email,
            CommitteeRoleId = row.r.IdCommitteeRole,
            CommitteeRoleCode = row.r.RoleCode,
            CommitteeRoleName = row.r.RoleName,
            EffectiveFromDate = row.m.EffectiveFromDate,
            EffectiveToDate = row.m.EffectiveToDate,
            StatusCode = row.s.StatusCode,
            StatusName = row.s.StatusName
        };
    }

    public async Task<int> AssignMemberAsync(
        int apartmentId, int userId, AssignCommitteeMemberRequest request, CancellationToken cancellationToken = default)
    {
        var ownerTypeId = await helper.GetOwnerPersonTypeIdAsync(cancellationToken);
        var p = await db.Persons
            .FirstOrDefaultAsync(
                x => x.IdPerson == request.PersonId && x.ApartmentId == apartmentId && x.PersonTypeId == ownerTypeId && x.IsActive,
                cancellationToken);
        if (p is null) throw new InvalidOperationException("Person must be an active owner in this apartment.");
        var t = await db.CommitteeTenures
            .FirstOrDefaultAsync(
                x => x.IdCommitteeTenure == request.CommitteeTenureId && x.ApartmentId == apartmentId, cancellationToken);
        if (t is null) throw new InvalidOperationException("Committee tenure not found.");
        var d = request.EffectiveFromDate.Date;
        if (d < t.TenureStartDate || d > t.TenureEndDate)
            throw new InvalidOperationException("EffectiveFromDate must be within the tenure start and end.");
        var role = await db.CommitteeRoles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdCommitteeRole == request.CommitteeRoleId && r.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Committee role not found.");
        var activeStatusId = await helper.GetStatusIdByCodeAsync(Committee.StatusActive, cancellationToken);
        var hasPersonRow = await db.CommitteeMembers.AnyAsync(
            m => m.ApartmentId == apartmentId
                 && m.CommitteeTenureId == request.CommitteeTenureId
                 && m.PersonId == request.PersonId
                 && m.StatusId == activeStatusId
                 && m.EffectiveFromDate <= DateTime.UtcNow.Date
                 && (m.EffectiveToDate == null || m.EffectiveToDate >= DateTime.UtcNow.Date),
            cancellationToken);
        if (hasPersonRow) throw new InvalidOperationException("This person already has an active committee assignment in this term.");
        if (Committee.IsSingletonRoleCode(role.RoleCode))
        {
            var taken = await db.CommitteeMembers.AnyAsync(
                m => m.ApartmentId == apartmentId
                     && m.CommitteeTenureId == request.CommitteeTenureId
                     && m.CommitteeRoleId == request.CommitteeRoleId
                     && m.StatusId == activeStatusId
                     && m.EffectiveFromDate <= DateTime.UtcNow.Date
                     && (m.EffectiveToDate == null || m.EffectiveToDate >= DateTime.UtcNow.Date),
                cancellationToken);
            if (taken) throw new InvalidOperationException($"The role {role.RoleCode} is already filled for this term.");
        }
        var now = DateTime.UtcNow;
        var row = new CommitteeMember
        {
            ApartmentId = apartmentId,
            CommitteeTenureId = request.CommitteeTenureId,
            PersonId = request.PersonId,
            CommitteeRoleId = request.CommitteeRoleId,
            EffectiveFromDate = d,
            StatusId = activeStatusId,
            CreatedAt = now,
            CreatedBy = userId
        };
        db.CommitteeMembers.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return row.IdCommitteeMember;
    }

    public async Task UpdateMemberAsync(
        int apartmentId, int userId, int id, UpdateCommitteeMemberRequest request, CancellationToken cancellationToken = default)
    {
        var m = await db.CommitteeMembers
            .FirstOrDefaultAsync(
                x => x.IdCommitteeMember == id && x.ApartmentId == apartmentId, cancellationToken);
        if (m is null) throw new InvalidOperationException("Committee member not found.");
        var t = await db.CommitteeTenures
            .FirstAsync(x => x.IdCommitteeTenure == m.CommitteeTenureId, cancellationToken);
        var d = request.EffectiveFromDate.Date;
        if (d < t.TenureStartDate || d > t.TenureEndDate)
            throw new InvalidOperationException("EffectiveFromDate must be within the tenure start and end.");
        var role = await db.CommitteeRoles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdCommitteeRole == request.CommitteeRoleId && r.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Committee role not found.");
        var activeStatusId = await helper.GetStatusIdByCodeAsync(Committee.StatusActive, cancellationToken);
        int statusId = m.StatusId;
        if (!string.IsNullOrWhiteSpace(request.StatusCode))
        {
            statusId = await helper.GetStatusIdByCodeAsync(request.StatusCode.Trim(), cancellationToken);
        }
        m.CommitteeRoleId = request.CommitteeRoleId;
        m.EffectiveFromDate = d;
        m.StatusId = statusId;
        m.UpdatedAt = DateTime.UtcNow;
        m.UpdatedBy = userId;
        if (Committee.IsSingletonRoleCode(role.RoleCode))
        {
            var taken = await db.CommitteeMembers.AnyAsync(
                x => x.ApartmentId == apartmentId
                     && x.CommitteeTenureId == m.CommitteeTenureId
                     && x.CommitteeRoleId == request.CommitteeRoleId
                     && x.IdCommitteeMember != id
                     && x.StatusId == activeStatusId
                     && x.EffectiveFromDate <= DateTime.UtcNow.Date
                     && (x.EffectiveToDate == null || x.EffectiveToDate >= DateTime.UtcNow.Date),
                cancellationToken);
            if (taken) throw new InvalidOperationException($"The role {role.RoleCode} is already filled for this term.");
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task EndMemberAsync(
        int apartmentId, int userId, int id, EndCommitteeMemberRequest request, CancellationToken cancellationToken = default)
    {
        var code = (request.EndStatusCode ?? string.Empty).Trim().ToUpperInvariant();
        if (code is not (Committee.StatusResigned or Committee.StatusRemoved))
            throw new InvalidOperationException("endStatusCode must be RESIGNED or REMOVED.");
        var m = await db.CommitteeMembers
            .FirstOrDefaultAsync(
                x => x.IdCommitteeMember == id && x.ApartmentId == apartmentId, cancellationToken);
        if (m is null) throw new InvalidOperationException("Committee member not found.");
        if (request.EndDate.Date < m.EffectiveFromDate.Date) throw new InvalidOperationException("endDate must be on or after effectiveFromDate.");
        var statusId = await helper.GetStatusIdByCodeAsync(code, cancellationToken);
        var now = DateTime.UtcNow;
        m.EffectiveToDate = request.EndDate.Date;
        m.StatusId = statusId;
        m.EndRemarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks!.Trim();
        m.UpdatedAt = now;
        m.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EligibleOwnerDto>> GetEligibleOwnersAsync(
        int apartmentId, int committeeTenureId, int? committeeRoleId, CancellationToken cancellationToken = default)
    {
        _ = committeeRoleId;
        _ = await db.CommitteeTenures
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.IdCommitteeTenure == committeeTenureId && t.ApartmentId == apartmentId, cancellationToken)
            ?? throw new InvalidOperationException("Committee tenure not found.");
        var ownerTypeId = await helper.GetOwnerPersonTypeIdAsync(cancellationToken);
        var activeStatusId = await helper.GetStatusIdByCodeAsync(Committee.StatusActive, cancellationToken);
        var today = DateTime.UtcNow.Date;
        var onTenure = await (from m in db.CommitteeMembers.AsNoTracking()
            where m.ApartmentId == apartmentId && m.CommitteeTenureId == committeeTenureId
                  && m.StatusId == activeStatusId
                  && m.EffectiveFromDate <= today
                  && (m.EffectiveToDate == null || m.EffectiveToDate >= today)
            select m.PersonId).Distinct().ToListAsync(cancellationToken);
        var onSet = onTenure.ToHashSet();
        var persons = await db.Persons.AsNoTracking()
            .Where(p => p.ApartmentId == apartmentId && p.PersonTypeId == ownerTypeId && p.IsActive)
            .OrderBy(p => p.FullName)
            .ToListAsync(cancellationToken);
        var list = new List<EligibleOwnerDto>(persons.Count);
        foreach (var p in persons)
        {
            var un = await helper.GetPrimaryUnitNumberAsync(apartmentId, p.IdPerson, cancellationToken);
            list.Add(new EligibleOwnerDto
            {
                PersonId = p.IdPerson,
                FullName = p.FullName,
                UnitNumber = un,
                PhoneNumber = p.PhoneNumber,
                IsAlreadyOnTenure = onSet.Contains(p.IdPerson)
            });
        }
        return list;
    }
}
