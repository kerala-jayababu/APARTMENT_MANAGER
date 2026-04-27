using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class BudgetService(AppDbContext db) : IBudgetService
{
    private const string StatusDraft = "DRAFT";

    public async Task<BudgetDetailDto> GetBudgetDetailAsync(
        int apartmentId, int fiscalYearId, CancellationToken cancellationToken = default)
    {
        var fy = await db.Set<FiscalYear>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdFiscalYear == fiscalYearId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fiscal year not found.");

        var header = await db.BudgetHeaders.AsNoTracking()
            .FirstOrDefaultAsync(h => h.ApartmentId == apartmentId && h.FiscalYearId == fiscalYearId && h.IsActive, cancellationToken)
            .ConfigureAwait(false);

        var previousFyId = await db.Set<FiscalYear>().AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.EndDate < fy.StartDate)
            .OrderByDescending(x => x.EndDate)
            .Select(x => (int?)x.IdFiscalYear)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var lines = await BuildBudgetLinesAsync(apartmentId, fiscalYearId, previousFyId, cancellationToken).ConfigureAwait(false);
        var totals = new BudgetTotalsDto
        {
            LastYrBudget = lines.Sum(x => x.LastYrBudget),
            LastYrActual = lines.Sum(x => x.LastYrActual),
            BudgetAmount = lines.Sum(x => x.BudgetAmount),
            ActualAmount = lines.Sum(x => x.ActualAmount)
        };

        var users = await LoadUserNamesAsync(apartmentId, header?.SubmittedByUserId, header?.ApprovedByUserId, cancellationToken)
            .ConfigureAwait(false);

        return new BudgetDetailDto
        {
            FiscalYearId = fiscalYearId,
            FyCode = fy.FyCode,
            Status = header?.StatusCode ?? StatusDraft,
            SubmittedAt = header?.SubmittedAt,
            SubmittedByName = users.GetValueOrDefault(header?.SubmittedByUserId),
            ApprovedAt = header?.ApprovedAt,
            ApprovedByName = users.GetValueOrDefault(header?.ApprovedByUserId),
            Lines = lines,
            Totals = totals
        };
    }

    public async Task<BudgetDetailDto> SaveBudgetLinesAsync(
        int apartmentId, int userId, int fiscalYearId, SaveBudgetRequest request, CancellationToken cancellationToken = default)
    {
        var lines = request.Lines ?? [];
        var duplicates = lines.GroupBy(x => x.ExpenseHeadId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0) throw new InvalidOperationException("Each expenseHeadId must be unique in request.");
        if (lines.Any(x => x.BudgetAmount < 0)) throw new InvalidOperationException("budgetAmount must be >= 0.");
        if (lines.Any(x => !string.IsNullOrWhiteSpace(x.Remarks) && x.Remarks!.Trim().Length > 200))
            throw new InvalidOperationException("remarks cannot exceed 200 characters.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;

        var header = await db.BudgetHeaders
            .FirstOrDefaultAsync(h => h.ApartmentId == apartmentId && h.FiscalYearId == fiscalYearId && h.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (header is null)
        {
            header = new BudgetHeader
            {
                ApartmentId = apartmentId,
                FiscalYearId = fiscalYearId,
                StatusCode = StatusDraft,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            };
            db.BudgetHeaders.Add(header);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (!string.Equals(header.StatusCode, StatusDraft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only DRAFT budgets can be modified.");
        }

        var existing = await db.Budgets
            .Where(b => b.ApartmentId == apartmentId && b.FiscalYearId == fiscalYearId && b.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingById = existing.ToDictionary(x => x.IdBudget, x => x);
        var existingByHead = existing.ToDictionary(x => x.ExpenseHeadId, x => x);
        var keepIds = new HashSet<int>();

        foreach (var line in lines)
        {
            Budget? target = null;
            if (line.Id is { } id && existingById.TryGetValue(id, out var byId))
                target = byId;
            else if (existingByHead.TryGetValue(line.ExpenseHeadId, out var byHead))
                target = byHead;

            if (target is null)
            {
                target = new Budget
                {
                    ApartmentId = apartmentId,
                    FiscalYearId = fiscalYearId,
                    ExpenseHeadId = line.ExpenseHeadId,
                    BudgetAmount = line.BudgetAmount,
                    ActualAmount = 0,
                    Remarks = string.IsNullOrWhiteSpace(line.Remarks) ? null : line.Remarks!.Trim(),
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = userId
                };
                db.Budgets.Add(target);
            }
            else
            {
                target.BudgetAmount = line.BudgetAmount;
                target.Remarks = string.IsNullOrWhiteSpace(line.Remarks) ? null : line.Remarks!.Trim();
                target.UpdatedAt = now;
                target.UpdatedBy = userId;
                target.IsActive = true;
            }

            if (target.IdBudget > 0) keepIds.Add(target.IdBudget);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var b in existing.Where(x => !keepIds.Contains(x.IdBudget)))
        {
            b.IsActive = false;
            b.UpdatedAt = now;
            b.UpdatedBy = userId;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        return await GetBudgetDetailAsync(apartmentId, fiscalYearId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<BudgetLineDetailDto>> BuildBudgetLinesAsync(
        int apartmentId,
        int fiscalYearId,
        int? previousFiscalYearId,
        CancellationToken cancellationToken)
    {
        var current = await db.Budgets.AsNoTracking()
            .Where(b => b.ApartmentId == apartmentId && b.FiscalYearId == fiscalYearId && b.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var headIds = current.Select(x => x.ExpenseHeadId).Distinct().ToList();
        if (headIds.Count == 0) return [];

        var prevByHead = previousFiscalYearId is null
            ? new Dictionary<int, Budget>()
            : await db.Budgets.AsNoTracking()
                .Where(b => b.ApartmentId == apartmentId && b.FiscalYearId == previousFiscalYearId && b.IsActive && headIds.Contains(b.ExpenseHeadId))
                .ToDictionaryAsync(x => x.ExpenseHeadId, x => x, cancellationToken)
                .ConfigureAwait(false);

        var heads = await db.ExpenseHeads.AsNoTracking()
            .Where(h => h.IsActive && headIds.Contains(h.IdExpenseHead))
            .ToDictionaryAsync(h => h.IdExpenseHead, h => h, cancellationToken)
            .ConfigureAwait(false);

        var mainHeadIds = heads.Values.Where(h => h.MainHeadId.HasValue).Select(h => h.MainHeadId!.Value).Distinct().ToList();
        var mainHeads = await db.ExpenseMainHeads.AsNoTracking()
            .Where(m => m.IsActive && mainHeadIds.Contains(m.IdExpenseMainHead) && (m.ApartmentId == null || m.ApartmentId == apartmentId))
            .ToDictionaryAsync(m => m.IdExpenseMainHead, m => m, cancellationToken)
            .ConfigureAwait(false);

        return current
            .OrderBy(x => heads.GetValueOrDefault(x.ExpenseHeadId)?.SortOrder ?? byte.MaxValue)
            .ThenBy(x => heads.GetValueOrDefault(x.ExpenseHeadId)?.HeadName)
            .Select(x =>
            {
                var h = heads.GetValueOrDefault(x.ExpenseHeadId);
                var mh = h?.MainHeadId is { } mid && mainHeads.TryGetValue(mid, out var m) ? m : null;
                var prev = prevByHead.GetValueOrDefault(x.ExpenseHeadId);
                return new BudgetLineDetailDto
                {
                    Id = x.IdBudget,
                    MainHeadId = h?.MainHeadId,
                    MainHeadName = mh?.MainHeadName,
                    ExpenseHeadId = x.ExpenseHeadId,
                    ExpenseHeadCode = h?.HeadCode ?? string.Empty,
                    ExpenseHeadName = h?.HeadName ?? string.Empty,
                    LastYrBudget = prev?.BudgetAmount ?? 0,
                    LastYrActual = prev?.ActualAmount ?? 0,
                    BudgetAmount = x.BudgetAmount,
                    ActualAmount = x.ActualAmount,
                    Remarks = x.Remarks
                };
            })
            .ToList();
    }

    private async Task<Dictionary<int?, string?>> LoadUserNamesAsync(
        int apartmentId, int? submittedById, int? approvedById, CancellationToken cancellationToken)
    {
        var ids = new[] { submittedById, approvedById }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int?, string?>();
        var names = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.IdUser))
            .ToDictionaryAsync(x => x.IdUser, x => x.FullName, cancellationToken)
            .ConfigureAwait(false);
        return new Dictionary<int?, string?>
        {
            [submittedById] = submittedById is { } s && names.TryGetValue(s, out var sn) ? sn : null,
            [approvedById] = approvedById is { } a && names.TryGetValue(a, out var an) ? an : null
        };
    }
}
