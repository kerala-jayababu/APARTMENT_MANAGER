using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation.Committee;

public sealed class CommitteeDataHelper(AppDbContext db)
{
    public async Task<int> GetStatusIdByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var c = code.Trim();
        var id = await db.CommitteeMemberStatuses.AsNoTracking()
            .Where(s => s.IsActive && s.StatusCode == c)
            .Select(s => s.IdCommitteeMemberStatus)
            .FirstOrDefaultAsync(cancellationToken);
        if (id == 0)
        {
            var all = await db.CommitteeMemberStatuses.AsNoTracking().Where(s => s.IsActive).ToListAsync(cancellationToken);
            var m = all.FirstOrDefault(s => string.Equals(s.StatusCode, c, StringComparison.OrdinalIgnoreCase));
            if (m is not null) id = m.IdCommitteeMemberStatus;
        }
        if (id == 0) throw new InvalidOperationException($"Status not found: {code}.");
        return id;
    }

    public async Task<int> GetOwnerPersonTypeIdAsync(CancellationToken cancellationToken = default)
    {
        var id = await db.PersonTypes.AsNoTracking()
            .Where(t => t.IsActive && t.PersonTypeCode.ToUpper() == PersonTypeCodes.PrimaryOwner.ToUpperInvariant())
            .Select(t => t.IdPersonType)
            .FirstOrDefaultAsync(cancellationToken);
        if (id == 0) throw new InvalidOperationException("OWNER person type is not configured.");
        return id;
    }

    public async Task<string?> GetPrimaryUnitNumberAsync(
        int apartmentId, int personId, CancellationToken cancellationToken) =>
        await (from uo in db.UnitOwners.AsNoTracking()
            where uo.ApartmentId == apartmentId && uo.PersonId == personId && uo.IsActive && uo.IsPrimaryOwner
            join u in db.Units.AsNoTracking() on uo.UnitId equals u.IdUnit
            select u.UnitNumber).FirstOrDefaultAsync(cancellationToken);

    public async Task<(Dictionary<int, int> Counts, Dictionary<int, (string? President, string? Secretary, string? Treasurer)>)>
        LoadMemberCountsAndKeyNamesAsync(
            int apartmentId, IReadOnlyList<int> tenureIds, int activeStatusId, DateTime today, CancellationToken cancellationToken)
    {
        if (tenureIds.Count == 0) return ([], []);
        var idList = tenureIds as List<int> ?? tenureIds.ToList();
        var sub = from m in db.CommitteeMembers.AsNoTracking()
            where m.ApartmentId == apartmentId && idList.Contains(m.CommitteeTenureId)
            join s in db.CommitteeMemberStatuses.AsNoTracking() on m.StatusId equals s.IdCommitteeMemberStatus
            join r in db.CommitteeRoles.AsNoTracking() on m.CommitteeRoleId equals r.IdCommitteeRole
            join p in db.Persons.AsNoTracking() on m.PersonId equals p.IdPerson
            where m.StatusId == activeStatusId
                  && m.EffectiveFromDate <= today
                  && (m.EffectiveToDate == null || m.EffectiveToDate >= today)
            select new
            {
                m.CommitteeTenureId,
                r.RoleCode,
                p.FullName
            };
        var list = await sub.ToListAsync(cancellationToken);
        var counts = list.GroupBy(x => x.CommitteeTenureId).ToDictionary(g => g.Key, g => g.Count());
        var pres = new Dictionary<int, string?>();
        var sec = new Dictionary<int, string?>();
        var tre = new Dictionary<int, string?>();
        foreach (var x in list)
        {
            if (string.Equals(x.RoleCode, Committee.RolePresident, StringComparison.OrdinalIgnoreCase)
                && !pres.ContainsKey(x.CommitteeTenureId))
                pres[x.CommitteeTenureId] = x.FullName;
            else if (string.Equals(x.RoleCode, Committee.RoleSecretary, StringComparison.OrdinalIgnoreCase)
                     && !sec.ContainsKey(x.CommitteeTenureId))
                sec[x.CommitteeTenureId] = x.FullName;
            else if (string.Equals(x.RoleCode, Committee.RoleTreasurer, StringComparison.OrdinalIgnoreCase)
                     && !tre.ContainsKey(x.CommitteeTenureId))
                tre[x.CommitteeTenureId] = x.FullName;
        }
        var key = new Dictionary<int, (string? President, string? Secretary, string? Treasurer)>();
        foreach (var tid in idList)
        {
            key[tid] = (pres.GetValueOrDefault(tid), sec.GetValueOrDefault(tid), tre.GetValueOrDefault(tid));
        }
        return (counts, key);
    }

    public async Task<IReadOnlyList<CommitteeMemberDetailItemDto>> GetMemberDetailItemsForTenureAsync(
        int apartmentId, int tenureId, CancellationToken cancellationToken)
    {
        var q = from m in db.CommitteeMembers.AsNoTracking()
            where m.ApartmentId == apartmentId && m.CommitteeTenureId == tenureId
            join r in db.CommitteeRoles.AsNoTracking() on m.CommitteeRoleId equals r.IdCommitteeRole
            join s in db.CommitteeMemberStatuses.AsNoTracking() on m.StatusId equals s.IdCommitteeMemberStatus
            join p in db.Persons.AsNoTracking() on m.PersonId equals p.IdPerson
            orderby r.SortOrder, p.FullName
            select new { m, r, s, p };
        var list = await q.ToListAsync(cancellationToken);
        var items = new List<CommitteeMemberDetailItemDto>();
        foreach (var x in list)
        {
            var un = await GetPrimaryUnitNumberAsync(apartmentId, x.p.IdPerson, cancellationToken);
            items.Add(new CommitteeMemberDetailItemDto
            {
                Id = x.m.IdCommitteeMember,
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
        return items;
    }
}
