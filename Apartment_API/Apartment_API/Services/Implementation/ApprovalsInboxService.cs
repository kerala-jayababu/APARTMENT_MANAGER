using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class ApprovalsInboxService(AppDbContext db) : IApprovalsInboxService
{
    private const string KindSetMmc = "SET_MMC";
    private const string KindBudgetHeader = "BUDGET_HEADER";
    private const string KindBudgetRevision = "BUDGET_REVISION";
    private const string KindAmenityBooking = "AMENITY_BOOKING";

    private const string MmcBatchPending = "PENDING";

    public async Task<ApprovalsInboxSummaryDto> GetSummaryAsync(int apartmentId, CancellationToken cancellationToken = default)
    {
        var pendingStatusIds = await GetPendingInvoiceStatusIdsAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow.Date;

        var mmc = await db.MmcBatches.AsNoTracking()
            .CountAsync(x => x.ApartmentId == apartmentId && x.IsActive && x.StatusCode == MmcBatchPending, cancellationToken)
            .ConfigureAwait(false);

        var budgetHeader = await db.BudgetHeaders.AsNoTracking()
            .CountAsync(x => x.ApartmentId == apartmentId && x.IsActive &&
                              x.SubmittedAt != null &&
                              x.ApprovedAt == null &&
                              x.RejectedAt == null &&
                              x.StatusCode != null &&
                              x.StatusCode != "DRAFT", cancellationToken)
            .ConfigureAwait(false);

        var budgetRevRows = await db.BudgetRevisions.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId &&
                        (x.ApprovalStatus == "PendingL1" || x.ApprovalStatus == "PendingL2"))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var budgetRev = budgetRevRows
            .GroupBy(x => new { x.FiscalYearId, x.CreatedBy, At = TruncateToSecondUtc(x.CreatedAt) })
            .Count();

        var budget = budgetHeader + budgetRev;

        var bookingQ = db.AmenityBookings.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IsActive);
        if (pendingStatusIds.Count > 0)
            bookingQ = bookingQ.Where(x => x.StatusId != null && pendingStatusIds.Contains(x.StatusId.Value));
        else
            bookingQ = bookingQ.Where(x => false);
        var bookings = await bookingQ.CountAsync(cancellationToken).ConfigureAwait(false);

        var total = mmc + budget + bookings;

        var aging = await CountAgingOver3DaysAsync(apartmentId, pendingStatusIds, now, cancellationToken).ConfigureAwait(false);

        return new ApprovalsInboxSummaryDto
        {
            TotalPending = total,
            SetMmc = mmc,
            Budget = budget,
            AmenityBookings = bookings,
            AgingOver3Days = aging
        };
    }

    public async Task<PagedResult<ApprovalInboxItemDto>> ListPendingAsync(
        int apartmentId,
        string? kindFilter,
        DateOnly? submittedFrom,
        DateOnly? submittedTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var k = (kindFilter ?? "all").Trim().ToLowerInvariant();
        var pendingStatusIds = await GetPendingInvoiceStatusIdsAsync(cancellationToken).ConfigureAwait(false);

        var fromDt = submittedFrom?.ToDateTime(TimeOnly.MinValue);
        var toDt = submittedTo?.ToDateTime(TimeOnly.MaxValue);

        var keys = new List<PendingKey>();
        if (k is "all" or "set_mmc")
        {
            var q = db.MmcBatches.AsNoTracking()
                .Where(x => x.ApartmentId == apartmentId && x.IsActive && x.StatusCode == MmcBatchPending);
            if (fromDt is { } f) q = q.Where(x => x.SubmittedAt >= f);
            if (toDt is { } t) q = q.Where(x => x.SubmittedAt <= t);
            keys.AddRange(await q.Select(x => new PendingKey(KindSetMmc, x.IdMmcBatch, x.SubmittedAt))
                .ToListAsync(cancellationToken).ConfigureAwait(false));
        }
        if (k is "all" or "budget")
        {
            var hq = db.BudgetHeaders.AsNoTracking()
                .Where(x => x.ApartmentId == apartmentId && x.IsActive && x.SubmittedAt != null &&
                            x.ApprovedAt == null && x.RejectedAt == null && x.StatusCode != null && x.StatusCode != "DRAFT");
            if (fromDt is { } f) hq = hq.Where(x => x.SubmittedAt >= f);
            if (toDt is { } t) hq = hq.Where(x => x.SubmittedAt <= t);
            keys.AddRange(await hq.Select(x => new PendingKey(KindBudgetHeader, x.IdBudgetHeader, x.SubmittedAt!.Value))
                .ToListAsync(cancellationToken).ConfigureAwait(false));

            var brLines = await db.BudgetRevisions.AsNoTracking()
                .Where(x => x.ApartmentId == apartmentId &&
                            (x.ApprovalStatus == "PendingL1" || x.ApprovalStatus == "PendingL2"))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            IEnumerable<PendingKey> brKeys = brLines
                .GroupBy(x => new { x.FiscalYearId, x.CreatedBy, At = TruncateToSecondUtc(x.CreatedAt) })
                .Select(g =>
                {
                    var firstId = g.Min(x => x.IdBudgetRevision);
                    var submittedAt = g.Max(x => x.ModifiedAt ?? x.CreatedAt);
                    return new PendingKey(KindBudgetRevision, firstId, submittedAt);
                });
            if (fromDt is { } f2) brKeys = brKeys.Where(k => k.SubmittedAt >= f2);
            if (toDt is { } t2) brKeys = brKeys.Where(k => k.SubmittedAt <= t2);
            keys.AddRange(brKeys.ToList());
        }
        if (k is "all" or "amenity_booking")
        {
            var bq = db.AmenityBookings.AsNoTracking()
                .Where(x => x.ApartmentId == apartmentId && x.IsActive);
            if (pendingStatusIds.Count > 0)
                bq = bq.Where(x => x.StatusId != null && pendingStatusIds.Contains(x.StatusId.Value));
            else
                bq = bq.Where(x => false);
            if (fromDt is { } f) bq = bq.Where(x => x.CreatedAt >= f);
            if (toDt is { } t) bq = bq.Where(x => x.CreatedAt <= t);
            keys.AddRange(await bq.Select(x => new PendingKey(KindAmenityBooking, x.IdAmenityBooking, x.CreatedAt))
                .ToListAsync(cancellationToken).ConfigureAwait(false));
        }

        var total = keys.Count;
        var pageItems = keys
            .OrderByDescending(x => x.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = new List<ApprovalInboxItemDto>(pageItems.Count);
        foreach (var pk in pageItems)
        {
            var dto = pk.Kind switch
            {
                KindSetMmc => await BuildMmcAsync(apartmentId, pk.SourceId, cancellationToken).ConfigureAwait(false),
                KindBudgetHeader => await BuildBudgetHeaderAsync(apartmentId, pk.SourceId, cancellationToken).ConfigureAwait(false),
                KindBudgetRevision => await BuildBudgetRevisionAsync(apartmentId, pk.SourceId, cancellationToken).ConfigureAwait(false),
                KindAmenityBooking => await BuildBookingAsync(apartmentId, pk.SourceId, cancellationToken).ConfigureAwait(false),
                _ => null
            };
            if (dto is not null) items.Add(dto);
        }

        return new PagedResult<ApprovalInboxItemDto>
            { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    private readonly record struct PendingKey(string Kind, int SourceId, DateTime SubmittedAt);

    private async Task<int> CountAgingOver3DaysAsync(
        int apartmentId,
        IReadOnlyList<int> pendingStatusIds,
        DateTime todayUtc,
        CancellationToken cancellationToken)
    {
        var mmc = await db.MmcBatches.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IsActive && x.StatusCode == MmcBatchPending &&
                        x.SubmittedAt.Date <= todayUtc.AddDays(-3))
            .CountAsync(cancellationToken).ConfigureAwait(false);

        var bh = await db.BudgetHeaders.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IsActive &&
                        x.SubmittedAt != null &&
                        x.ApprovedAt == null &&
                        x.RejectedAt == null &&
                        x.StatusCode != null &&
                        x.StatusCode != "DRAFT" &&
                        x.SubmittedAt!.Value.Date <= todayUtc.AddDays(-3))
            .CountAsync(cancellationToken).ConfigureAwait(false);

        var brPending = await db.BudgetRevisions.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId &&
                        (x.ApprovalStatus == "PendingL1" || x.ApprovalStatus == "PendingL2"))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var br = brPending
            .GroupBy(x => new { x.FiscalYearId, x.CreatedBy, At = TruncateToSecondUtc(x.CreatedAt) })
            .Count(g =>
            {
                var submittedAt = g.Max(x => x.ModifiedAt ?? x.CreatedAt);
                return submittedAt.Date <= todayUtc.AddDays(-3);
            });

        var bq = db.AmenityBookings.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.IsActive &&
                        x.CreatedAt.Date <= todayUtc.AddDays(-3));
        if (pendingStatusIds.Count > 0)
            bq = bq.Where(x => x.StatusId != null && pendingStatusIds.Contains(x.StatusId.Value));
        else
            bq = bq.Where(x => false);
        var bk = await bq.CountAsync(cancellationToken).ConfigureAwait(false);

        return mmc + bh + br + bk;
    }

    private async Task<List<int>> GetPendingInvoiceStatusIdsAsync(CancellationToken cancellationToken) =>
        await db.InvoiceStatuses.AsNoTracking()
            .Where(x => x.IsActive && (x.StatusCode == "PENDING" || x.StatusCode == "PENDING_PAYMENT"))
            .Select(x => x.IdInvoiceStatus)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    private async Task<ApprovalInboxItemDto?> BuildMmcAsync(int apartmentId, int id, CancellationToken cancellationToken)
    {
        var b = await db.MmcBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdMmcBatch == id, cancellationToken)
            .ConfigureAwait(false);
        if (b is null) return null;
        var period = await db.MmcPeriods.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdMmcPeriod == b.MmcPeriodId, cancellationToken).ConfigureAwait(false);
        var lines = await db.MmcBatchLines.AsNoTracking()
            .Where(x => x.MmcBatchId == id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var submitter = await db.Users.AsNoTracking()
            .Where(u => u.IdUser == b.SubmittedByUserId).Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var flow = await ResolveFlowAsync(["MMC_BATCH", "SET_MMC", "MMC"], cancellationToken).ConfigureAwait(false);
        var periodName = period?.PeriodName ?? string.Empty;
        var lineCount = lines.Count;
        var delta = lines.Sum(x => x.NewMmcAmount - x.PreviousMmcAmount);
        var amount = lines.Sum(x => x.NewMmcAmount);
        var subject = $"{periodName} · {lineCount} units · Δ ₹{delta:N0}";
        var year = b.SubmittedAt.Year;
        return new ApprovalInboxItemDto
        {
            Kind = KindSetMmc,
            SourceId = id,
            ApprovalReference = FormatAr(year, id),
            SecondaryReference = null,
            TypeLabel = "Set MMC",
            Subject = subject.Trim(),
            SubmittedBy = submitter,
            SubmittedAt = b.SubmittedAt,
            Approver = flow,
            Amount = amount,
            AgingDays = AgingDaysUtc(b.SubmittedAt),
            StatusLabel = "Pending",
            WorkflowStage = FlowHasTwoSteps(flow.FlowCode) ? "L1" : null
        };
    }

    private async Task<ApprovalInboxItemDto?> BuildBudgetHeaderAsync(int apartmentId, int id, CancellationToken cancellationToken)
    {
        var h = await db.BudgetHeaders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdBudgetHeader == id, cancellationToken)
            .ConfigureAwait(false);
        if (h is null) return null;
        var fy = await db.FiscalYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdFiscalYear == h.FiscalYearId, cancellationToken).ConfigureAwait(false);
        var total = await db.Budgets.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.FiscalYearId == h.FiscalYearId && x.IsActive)
            .SumAsync(x => x.BudgetAmount, cancellationToken).ConfigureAwait(false);
        var submitter = h.SubmittedByUserId is { } su
            ? await db.Users.AsNoTracking().Where(u => u.IdUser == su).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false) ?? string.Empty
            : string.Empty;
        var flow = await ResolveFlowAsync(["BUDGET", "BUDGET_HEADER"], cancellationToken).ConfigureAwait(false);
        var fyLabel = fy?.FyCode ?? $"FY{h.FiscalYearId}";
        var submittedAt = h.SubmittedAt ?? h.CreatedAt;
        var year = submittedAt.Year;
        return new ApprovalInboxItemDto
        {
            Kind = KindBudgetHeader,
            SourceId = id,
            ApprovalReference = FormatAr(year, id),
            SecondaryReference = null,
            TypeLabel = "Budget",
            Subject = $"Annual budget · {fyLabel} · v1 · ₹{total:N0} total",
            SubmittedBy = submitter,
            SubmittedAt = submittedAt,
            Approver = flow,
            Amount = total,
            AgingDays = AgingDaysUtc(submittedAt),
            StatusLabel = "Pending",
            WorkflowStage = FlowHasTwoSteps(flow.FlowCode) ? "L1" : null
        };
    }

    private async Task<ApprovalInboxItemDto?> BuildBudgetRevisionAsync(int apartmentId, int representativeLineId,
        CancellationToken cancellationToken)
    {
        var seed = await db.BudgetRevisions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdBudgetRevision == representativeLineId,
                cancellationToken).ConfigureAwait(false);
        if (seed is null) return null;
        var tsLo = TruncateToSecondUtc(seed.CreatedAt);
        var tsHi = tsLo.AddSeconds(1);
        var batchLines = await db.BudgetRevisions.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.FiscalYearId == seed.FiscalYearId &&
                        x.CreatedBy == seed.CreatedBy && x.CreatedAt >= tsLo && x.CreatedAt < tsHi)
            .OrderBy(x => x.IdBudgetRevision)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (batchLines.Count == 0) return null;
        var first = batchLines[0];
        var st = (first.ApprovalStatus ?? "").Trim();
        if (st is not ("PendingL1" or "PendingL2")) return null;

        var fy = await db.FiscalYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdFiscalYear == first.FiscalYearId, cancellationToken).ConfigureAwait(false);
        var fyLabel = fy?.FyCode ?? $"FY{first.FiscalYearId}";
        var submitter = await db.Users.AsNoTracking()
            .Where(u => u.IdUser == first.CreatedBy).Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
        var flow = await ResolveFlowAsync(["BUDGET_REVISION", "BUDGET"], cancellationToken).ConfigureAwait(false);
        var title = string.IsNullOrWhiteSpace(first.RevisionTitle) ? "Budget revision" : first.RevisionTitle.Trim();
        var delta = batchLines.Sum(x => x.RevisedAmount - x.OriginalAmount);
        var subject = $"{title} · {fyLabel} · {batchLines.Count} line(s) · Δ ₹{delta:N0}";
        var submittedAt = batchLines.Max(x => x.ModifiedAt ?? x.CreatedAt);
        var year = submittedAt.Year;
        var workflow = st == "PendingL1" ? "L1" : st == "PendingL2" ? "L2" : null;
        return new ApprovalInboxItemDto
        {
            Kind = KindBudgetRevision,
            SourceId = representativeLineId,
            ApprovalReference = FormatAr(year, representativeLineId),
            SecondaryReference = null,
            TypeLabel = "Budget",
            Subject = subject.Trim(),
            SubmittedBy = submitter,
            SubmittedAt = submittedAt,
            Approver = flow,
            Amount = batchLines.Sum(x => x.RevisedAmount),
            AgingDays = AgingDaysUtc(submittedAt),
            StatusLabel = "Pending",
            WorkflowStage = workflow
        };
    }

    private static DateTime TruncateToSecondUtc(DateTime dt)
    {
        var u = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
        return new DateTime(u.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }

    private async Task<ApprovalInboxItemDto?> BuildBookingAsync(int apartmentId, int id, CancellationToken cancellationToken)
    {
        var b = await db.AmenityBookings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdAmenityBooking == id, cancellationToken)
            .ConfigureAwait(false);
        if (b is null) return null;
        var a = await db.Amenities.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdAmenity == b.AmenityId, cancellationToken).ConfigureAwait(false);
        var u = await db.Units.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdUnit == b.UnitId, cancellationToken).ConfigureAwait(false);
        var p = await db.Persons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdPerson == b.PersonId, cancellationToken).ConfigureAwait(false);
        var flow = await ResolveFlowAsync(["AMENITY_BOOKING", "BOOKING"], cancellationToken).ConfigureAwait(false);
        var bk = $"BK-{b.BookingDate.Year}-{b.IdAmenityBooking:D4}";
        var subject = $"{bk} · {a?.AmenityName ?? "Amenity"} · {u?.UnitNumber ?? "?"} · {b.BookingDate:yyyy-MM-dd}";
        var year = b.CreatedAt.Year;
        return new ApprovalInboxItemDto
        {
            Kind = KindAmenityBooking,
            SourceId = id,
            ApprovalReference = FormatAr(year, id),
            SecondaryReference = bk,
            TypeLabel = "Booking",
            Subject = subject,
            SubmittedBy = p?.FullName ?? string.Empty,
            SubmittedAt = b.CreatedAt,
            Approver = flow,
            Amount = b.ChargeAmount,
            AgingDays = AgingDaysUtc(b.CreatedAt),
            StatusLabel = "Pending",
            WorkflowStage = null
        };
    }

    private async Task<ApprovalApproverHintDto> ResolveFlowAsync(string[] entityCodes, CancellationToken cancellationToken)
    {
        var flows = await db.ApprovalFlows.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var code in entityCodes)
        {
            var row = flows.FirstOrDefault(x =>
                x.EntityCode != null && string.Equals(x.EntityCode, code, StringComparison.OrdinalIgnoreCase));
            if (row?.FlowCode is { Length: > 0 } fc)
                return new ApprovalApproverHintDto { FlowCode = fc, Summary = HumanizeFlow(fc) };
        }
        return new ApprovalApproverHintDto { FlowCode = "S,P", Summary = "Secretary & President" };
    }

    private static string HumanizeFlow(string flowCode)
    {
        var parts = flowCode.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var labels = new List<string>();
        foreach (var p in parts)
        {
            var u = p.ToUpperInvariant();
            if (u is "S" or "SECRETARY") labels.Add("Secretary");
            else if (u is "P" or "PRESIDENT") labels.Add("President");
            else labels.Add(p);
        }
        var summary = labels.Count switch
        {
            0 => "Approver",
            1 => labels[0],
            2 => $"{labels[0]} & {labels[1]}",
            _ => string.Join(", ", labels)
        };
        return summary;
    }

    private static bool FlowHasTwoSteps(string flowCode)
    {
        var n = flowCode.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length;
        return n >= 2;
    }

    private static string FormatAr(int year, int id) => $"AR-{year}-{id:D4}";

    private static int AgingDaysUtc(DateTime submittedUtc)
    {
        var a = DateOnly.FromDateTime(DateTime.UtcNow);
        var b = DateOnly.FromDateTime(submittedUtc.Kind == DateTimeKind.Utc ? submittedUtc : submittedUtc.ToUniversalTime());
        return Math.Max(0, a.DayNumber - b.DayNumber);
    }
}
