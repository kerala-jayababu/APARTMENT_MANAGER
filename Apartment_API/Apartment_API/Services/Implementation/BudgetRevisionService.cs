using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class BudgetRevisionService(AppDbContext db, IWebHostEnvironment env) : IBudgetRevisionService
{
    private const long MaxPdfBytes = 5 * 1024 * 1024;
    private const int MaxPerHead = 2;
    private const int MinReason = 20;
    private const int MaxReason = 500;
    private const int MinJustification = 50;
    private const int MaxJustification = 2000;
    private const int MaxTitle = 100;

    private static readonly string[] CountStatuses =
        ["Draft", "PendingL1", "PendingL2", "Active"];

    public async Task<PagedResult<BudgetRevisionBatchListItemDto>> ListBatchesAsync(
        int apartmentId,
        int? fiscalYearId,
        string? status,
        int? budgetId,
        int? createdBy,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = db.BudgetRevisions.AsNoTracking().Where(x => x.ApartmentId == apartmentId);
        if (fiscalYearId is { } fy) q = q.Where(x => x.FiscalYearId == fy);
        if (createdBy is { } cb) q = q.Where(x => x.CreatedBy == cb);
        if (from is { } f) q = q.Where(x => x.CreatedAt >= f.ToDateTime(TimeOnly.MinValue));
        if (to is { } t) q = q.Where(x => x.CreatedAt <= t.ToDateTime(TimeOnly.MaxValue));
        if (budgetId is { } bid) q = q.Where(x => x.BudgetId == bid);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var want = NormalizeStatus(status.Trim());
            q = q.Where(x => NormalizeStatus(x.ApprovalStatus) == want);
        }
        var rows = await q.ToListAsync(cancellationToken).ConfigureAwait(false);
        var batchGroups = rows
            .GroupBy(x => new { x.ApartmentId, x.FiscalYearId, x.CreatedBy, x.CreatedAt })
            .Select(g => g.OrderBy(x => x.IdBudgetRevision).ToList())
            .OrderByDescending(g => g[0].CreatedAt)
            .ToList();
        var total = batchGroups.Count;
        var pageGroups = batchGroups.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var items = new List<BudgetRevisionBatchListItemDto>();
        foreach (var g in pageGroups)
        {
            items.Add(await MapBatchListItemAsync(apartmentId, g, cancellationToken).ConfigureAwait(false));
        }
        return new PagedResult<BudgetRevisionBatchListItemDto>
            { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<BudgetRevisionBatchDetailDto?> GetBatchAsync(int apartmentId, string batchId, CancellationToken cancellationToken = default)
    {
        var lines = await LoadBatchLinesAsync(apartmentId, batchId, cancellationToken).ConfigureAwait(false);
        if (lines.Count == 0) return null;
        return await MapBatchDetailAsync(apartmentId, lines, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BudgetRevisionLineDetailDto?> GetLineAsync(int apartmentId, int idBudgetRevision, CancellationToken cancellationToken = default)
    {
        var line = await db.BudgetRevisions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdBudgetRevision == idBudgetRevision, cancellationToken)
            .ConfigureAwait(false);
        if (line is null) return null;
        var batchId = BudgetRevisionBatchId.Encode(line.ApartmentId, line.FiscalYearId, line.CreatedBy, line.CreatedAt);
        return await MapLineDetailAsync(line, batchId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CreateBudgetRevisionBatchResultDto> CreateDraftAsync(
        int apartmentId, int userId, CreateBudgetRevisionBatchRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateBody(request);
        await AssertAnnualBudgetActiveAsync(apartmentId, request.FiscalYearId, cancellationToken).ConfigureAwait(false);
        await ValidateLineBudgetsAndQuotaAsync(apartmentId, request.FiscalYearId, request.Lines, null, cancellationToken)
            .ConfigureAwait(false);

        var now = TruncateToSecondsUtc(DateTime.UtcNow);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var lines = new List<BudgetRevision>();
        foreach (var ln in request.Lines)
        {
            var b = await RequireBudgetAsync(apartmentId, request.FiscalYearId, ln.BudgetId, cancellationToken).ConfigureAwait(false);
            if (ln.RevisedAmount <= 0) throw new InvalidOperationException("revisedAmount must be > 0.");
            if (ln.RevisedAmount == b.BudgetAmount) throw new InvalidOperationException("revisedAmount must differ from current budget amount.");
            var row = new BudgetRevision
            {
                ApartmentId = apartmentId,
                BudgetId = ln.BudgetId,
                FiscalYearId = request.FiscalYearId,
                OriginalAmount = b.BudgetAmount,
                RevisedAmount = ln.RevisedAmount,
                ReasonForRevision = ln.ReasonForRevision.Trim(),
                CreatedBy = userId,
                CreatedAt = now,
                ApprovalStatus = "Draft",
                IsLocked = "N",
                RevisionTitle = request.RevisionTitle.Trim(),
                OverallJustification = request.OverallJustification.Trim(),
                SupportingDocUrl = string.IsNullOrWhiteSpace(request.SupportingDocUrl) ? null : request.SupportingDocUrl.Trim()
            };
            db.BudgetRevisions.Add(row);
            lines.Add(row);
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return MapCreateResult(lines);
    }

    public async Task<CreateBudgetRevisionBatchResultDto> ReplaceDraftBatchAsync(
        int apartmentId, int userId, string batchId, UpdateBudgetRevisionBatchRequest request, CancellationToken cancellationToken = default)
    {
        if (!BudgetRevisionBatchId.TryDecode(batchId, out var apt, out var fy, out var creator, out var createdAt) || apt != apartmentId)
            throw new InvalidOperationException("Invalid batchId.");
        ValidateUpdateBody(request);
        var existing = await LoadBatchLinesAsync(apartmentId, batchId, cancellationToken).ConfigureAwait(false);
        if (existing.Count == 0) throw new InvalidOperationException("Batch not found.");
        if (existing.Any(x => NormalizeStatus(x.ApprovalStatus) != "Draft")) throw new InvalidOperationException("CONFLICT: Only Draft batches can be replaced.");
        if (existing[0].CreatedBy != userId) throw new UnauthorizedAccessException("FORBIDDEN");
        await AssertAnnualBudgetActiveAsync(apartmentId, fy, cancellationToken).ConfigureAwait(false);
        await ValidateLineBudgetsAndQuotaAsync(apartmentId, fy, request.Lines, existing, cancellationToken).ConfigureAwait(false);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        foreach (var e in existing)
            db.BudgetRevisions.Remove(e);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var lines = new List<BudgetRevision>();
        foreach (var ln in request.Lines)
        {
            var b = await RequireBudgetAsync(apartmentId, fy, ln.BudgetId, cancellationToken).ConfigureAwait(false);
            if (ln.RevisedAmount <= 0) throw new InvalidOperationException("revisedAmount must be > 0.");
            if (ln.RevisedAmount == b.BudgetAmount) throw new InvalidOperationException("revisedAmount must differ from current budget amount.");
            var row = new BudgetRevision
            {
                ApartmentId = apartmentId,
                BudgetId = ln.BudgetId,
                FiscalYearId = fy,
                OriginalAmount = b.BudgetAmount,
                RevisedAmount = ln.RevisedAmount,
                ReasonForRevision = ln.ReasonForRevision.Trim(),
                CreatedBy = creator,
                CreatedAt = TruncateToSecondsUtc(createdAt),
                ModifiedBy = userId,
                ModifiedAt = DateTime.UtcNow,
                ApprovalStatus = "Draft",
                IsLocked = "N",
                RevisionTitle = request.RevisionTitle.Trim(),
                OverallJustification = request.OverallJustification.Trim(),
                SupportingDocUrl = string.IsNullOrWhiteSpace(request.SupportingDocUrl) ? null : request.SupportingDocUrl.Trim()
            };
            db.BudgetRevisions.Add(row);
            lines.Add(row);
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return MapCreateResult(lines);
    }

    public async Task<BudgetRevisionLineDetailDto> PatchLineAsync(
        int apartmentId, int userId, int idBudgetRevision, PatchBudgetRevisionLineRequest request, CancellationToken cancellationToken = default)
    {
        var row = await db.BudgetRevisions
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdBudgetRevision == idBudgetRevision, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Line not found.");
        if (NormalizeStatus(row.ApprovalStatus) != "Draft") throw new InvalidOperationException("CONFLICT: Only Draft lines can be edited.");
        if (string.Equals(row.IsLocked, "Y", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CONFLICT: Line is locked.");
        if (request.RevisedAmount is null && string.IsNullOrWhiteSpace(request.ReasonForRevision))
            throw new InvalidOperationException("Provide revisedAmount and/or reasonForRevision.");
        var budget = await RequireBudgetAsync(apartmentId, row.FiscalYearId, row.BudgetId, cancellationToken).ConfigureAwait(false);
        if (request.RevisedAmount is { } ra)
        {
            if (ra <= 0) throw new InvalidOperationException("revisedAmount must be > 0.");
            if (ra == row.OriginalAmount) throw new InvalidOperationException("revisedAmount must differ from original snapshot.");
            row.RevisedAmount = ra;
        }
        if (!string.IsNullOrWhiteSpace(request.ReasonForRevision))
        {
            var r = request.ReasonForRevision.Trim();
            if (r.Length is < MinReason or > MaxReason) throw new InvalidOperationException($"reasonForRevision must be {MinReason}..{MaxReason} chars.");
            row.ReasonForRevision = r;
        }
        row.ModifiedBy = userId;
        row.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var bid = BudgetRevisionBatchId.Encode(row.ApartmentId, row.FiscalYearId, row.CreatedBy, row.CreatedAt);
        return (await MapLineDetailAsync(row, bid, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task DeleteLineAsync(int apartmentId, int userId, int idBudgetRevision, CancellationToken cancellationToken = default)
    {
        var row = await db.BudgetRevisions
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdBudgetRevision == idBudgetRevision, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Line not found.");
        if (NormalizeStatus(row.ApprovalStatus) != "Draft") throw new InvalidOperationException("CONFLICT: Only Draft lines can be deleted.");
        db.BudgetRevisions.Remove(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteBatchAsync(int apartmentId, int userId, string batchId, CancellationToken cancellationToken = default)
    {
        var lines = await LoadBatchLinesTrackedAsync(apartmentId, batchId, cancellationToken).ConfigureAwait(false);
        if (lines.Count == 0) throw new InvalidOperationException("Batch not found.");
        if (lines.Any(x => NormalizeStatus(x.ApprovalStatus) != "Draft")) throw new InvalidOperationException("CONFLICT: Only Draft batches can be deleted.");
        if (lines[0].CreatedBy != userId) throw new UnauthorizedAccessException("FORBIDDEN");
        db.BudgetRevisions.RemoveRange(lines);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BudgetRevisionSubmitResultDto> SubmitAsync(
        int apartmentId, int userId, string batchId, BudgetRevisionSubmitRequest request, CancellationToken cancellationToken = default)
    {
        _ = request;
        var lines = await LoadBatchLinesTrackedAsync(apartmentId, batchId, cancellationToken).ConfigureAwait(false);
        if (lines.Count == 0) throw new InvalidOperationException("Batch not found.");
        if (lines.Any(x => NormalizeStatus(x.ApprovalStatus) != "Draft")) throw new InvalidOperationException("CONFLICT: Only Draft batches can be submitted.");
        if (lines[0].CreatedBy != userId) throw new UnauthorizedAccessException("FORBIDDEN");
        var now = DateTime.UtcNow;
        foreach (var x in lines)
        {
            x.ApprovalStatus = "PendingL1";
            x.ModifiedBy = userId;
            x.ModifiedAt = now;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var enc = BudgetRevisionBatchId.Encode(lines[0].ApartmentId, lines[0].FiscalYearId, lines[0].CreatedBy, lines[0].CreatedAt);
        return new BudgetRevisionSubmitResultDto
        {
            BatchId = enc,
            ApprovalStatus = "PendingL1",
            ApprovalRef = new BudgetRevisionApprovalRefLiteDto { ApprovalId = FormatApprovalRef(lines[0]) },
            SubmittedAt = now
        };
    }

    public async Task<BudgetRevisionBatchDetailDto> RecallAsync(
        int apartmentId, int userId, string batchId, BudgetRevisionRecallRequest request, CancellationToken cancellationToken = default)
    {
        _ = request;
        var lines = await LoadBatchLinesTrackedAsync(apartmentId, batchId, cancellationToken).ConfigureAwait(false);
        if (lines.Count == 0) throw new InvalidOperationException("Batch not found.");
        var st = NormalizeStatus(lines[0].ApprovalStatus);
        if (st is not ("PendingL1" or "PendingL2")) throw new InvalidOperationException("CONFLICT: Only pending batches can be recalled.");
        if (lines[0].CreatedBy != userId) throw new UnauthorizedAccessException("FORBIDDEN");
        var now = DateTime.UtcNow;
        foreach (var x in lines)
        {
            x.ApprovalStatus = "Draft";
            x.ModifiedBy = userId;
            x.ModifiedAt = now;
            x.L1ApprovedAt = null;
            x.L1ApprovedByUserId = null;
            x.L2ApprovedAt = null;
            x.L2ApprovedByUserId = null;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (await GetBatchAsync(apartmentId, batchId, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<BudgetRevisionApproveResultDto> ApproveAsync(
        int apartmentId, int userId, string batchId, BudgetRevisionApproveRequest request, CancellationToken cancellationToken = default)
    {
        var lines = await LoadBatchLinesTrackedAsync(apartmentId, batchId, cancellationToken).ConfigureAwait(false);
        if (lines.Count == 0) throw new InvalidOperationException("Batch not found.");
        var cur = NormalizeStatus(lines[0].ApprovalStatus);
        var level = (request.Level ?? "").Trim().ToUpperInvariant();
        if (level is not ("L1" or "L2"))
        {
            level = cur == "PendingL1" ? "L1" : cur == "PendingL2" ? "L2" : "";
        }
        if (level is not ("L1" or "L2")) throw new InvalidOperationException("Cannot infer approval level.");
        if (cur == "PendingL1" && level != "L1") throw new InvalidOperationException("CONFLICT: Batch is awaiting L1 approval.");
        if (cur == "PendingL2" && level != "L2") throw new InvalidOperationException("CONFLICT: Batch is awaiting L2 approval.");
        if (cur is not ("PendingL1" or "PendingL2")) throw new InvalidOperationException("CONFLICT: Batch is not pending approval.");

        var role = await GetApartmentRoleCodeAsync(userId, apartmentId, cancellationToken).ConfigureAwait(false);
        if (level == "L1" && !IsSecretary(role)) throw new UnauthorizedAccessException("FORBIDDEN");
        if (level == "L2" && !IsPresident(role)) throw new UnauthorizedAccessException("FORBIDDEN");

        var now = DateTime.UtcNow;
        if (level == "L1")
        {
            foreach (var x in lines)
            {
                x.ApprovalStatus = "PendingL2";
                x.L1ApprovedAt = now;
                x.L1ApprovedByUserId = userId;
                x.ModifiedAt = now;
                x.ModifiedBy = userId;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new BudgetRevisionApproveResultDto
            {
                BatchId = batchId,
                ApprovalStatus = "PendingL2",
                IsLocked = false,
                L1ApprovedAt = now,
                L2ApprovedAt = null,
                LockedRows = 0,
                BudgetsUpdated = []
            };
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var updated = new List<BudgetUpdatedDto>();
        foreach (var rev in lines)
        {
            var b = await db.Budgets.FirstAsync(x => x.IdBudget == rev.BudgetId && x.ApartmentId == apartmentId, cancellationToken)
                .ConfigureAwait(false);
            var from = b.BudgetAmount;
            b.BudgetAmount = rev.RevisedAmount;
            b.UpdatedAt = now;
            b.UpdatedBy = userId;
            rev.ApprovalStatus = "Active";
            rev.IsLocked = "Y";
            rev.L2ApprovedAt = now;
            rev.L2ApprovedByUserId = userId;
            rev.ModifiedAt = now;
            rev.ModifiedBy = userId;
            updated.Add(new BudgetUpdatedDto { BudgetId = b.IdBudget, FromAmount = from, ToAmount = rev.RevisedAmount });
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new BudgetRevisionApproveResultDto
        {
            BatchId = batchId,
            ApprovalStatus = "Active",
            IsLocked = true,
            L1ApprovedAt = lines[0].L1ApprovedAt,
            L2ApprovedAt = now,
            LockedRows = lines.Count,
            BudgetsUpdated = updated
        };
    }

    public async Task<BudgetRevisionRejectResultDto> RejectAsync(
        int apartmentId, int userId, string batchId, BudgetRevisionRejectRequest request, CancellationToken cancellationToken = default)
    {
        var reason = (request.Reason ?? "").Trim();
        if (reason.Length is < 1 or > 500) throw new InvalidOperationException("reason must be 1..500 characters.");
        var lines = await LoadBatchLinesTrackedAsync(apartmentId, batchId, cancellationToken).ConfigureAwait(false);
        if (lines.Count == 0) throw new InvalidOperationException("Batch not found.");
        var cur = NormalizeStatus(lines[0].ApprovalStatus);
        if (cur is not ("PendingL1" or "PendingL2")) throw new InvalidOperationException("CONFLICT: Only pending batches can be rejected.");
        var role = await GetApartmentRoleCodeAsync(userId, apartmentId, cancellationToken).ConfigureAwait(false);
        if (cur == "PendingL1" && !IsSecretary(role)) throw new UnauthorizedAccessException("FORBIDDEN");
        if (cur == "PendingL2" && !IsPresident(role)) throw new UnauthorizedAccessException("FORBIDDEN");
        var now = DateTime.UtcNow;
        foreach (var x in lines)
        {
            x.ApprovalStatus = "Rejected";
            x.RejectedAt = now;
            x.RejectedByUserId = userId;
            x.RejectionReason = reason;
            x.ModifiedAt = now;
            x.ModifiedBy = userId;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new BudgetRevisionRejectResultDto { BatchId = batchId, ApprovalStatus = "Rejected", RejectedAt = now };
    }

    public async Task<BudgetRevisionFileUploadDto> UploadFileAsync(
        int apartmentId, int userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        _ = apartmentId;
        _ = userId;
        if (file.Length == 0) throw new InvalidOperationException("file is required.");
        if (file.Length > MaxPdfBytes) throw new InvalidOperationException("FILE_TOO_LARGE");
        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("FILE_TYPE_NOT_ALLOWED");
        var mime = string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType;
        if (!mime.Contains("pdf", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("FILE_TYPE_NOT_ALLOWED");

        var now = DateTime.UtcNow;
        var key = $"budget-rev/{apartmentId}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}.pdf";
        var fullPath = Path.Combine(env.ContentRootPath, "Uploads", key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = File.Create(fullPath))
        {
            await file.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }
        var url = "/uploads/" + key.Replace('\\', '/');
        return new BudgetRevisionFileUploadDto { Url = url, Size = file.Length, Mime = mime };
    }

    public async Task<EligibleBudgetListDto> GetEligibleBudgetsAsync(int apartmentId, int fiscalYearId, CancellationToken cancellationToken = default)
    {
        if (!await IsAnnualBudgetActiveAsync(apartmentId, fiscalYearId, cancellationToken).ConfigureAwait(false))
            return new EligibleBudgetListDto { Items = [], Total = 0 };

        var budgets = await db.Budgets.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.FiscalYearId == fiscalYearId && x.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (budgets.Count == 0) return new EligibleBudgetListDto { Items = [], Total = 0 };

        var headIds = budgets.Select(x => x.ExpenseHeadId).Distinct().ToList();
        var heads = await db.ExpenseHeads.AsNoTracking().Where(x => headIds.Contains(x.IdExpenseHead))
            .ToDictionaryAsync(x => x.IdExpenseHead, x => x, cancellationToken).ConfigureAwait(false);
        var mainIds = heads.Values.Where(h => h.MainHeadId.HasValue).Select(h => h.MainHeadId!.Value).Distinct().ToList();
        var mains = await db.ExpenseMainHeads.AsNoTracking()
            .Where(m => mainIds.Contains(m.IdExpenseMainHead))
            .ToDictionaryAsync(x => x.IdExpenseMainHead, x => x.MainHeadName, cancellationToken).ConfigureAwait(false);

        var items = new List<EligibleBudgetDto>();
        foreach (var b in budgets.OrderBy(x => heads.GetValueOrDefault(x.ExpenseHeadId)?.SortOrder ?? 255))
        {
            var used = await CountRevisionsForBudgetAsync(apartmentId, fiscalYearId, b.IdBudget, cancellationToken).ConfigureAwait(false);
            var lastOn = await db.BudgetRevisions.AsNoTracking()
                .Where(x => x.ApartmentId == apartmentId && x.FiscalYearId == fiscalYearId && x.BudgetId == b.IdBudget &&
                            x.ApprovalStatus == "Active")
                .OrderByDescending(x => x.ModifiedAt ?? x.CreatedAt)
                .Select(x => (DateTime?)x.L2ApprovedAt ?? x.ModifiedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            var h = heads.GetValueOrDefault(b.ExpenseHeadId);
            var mhName = h?.MainHeadId is { } mid && mains.TryGetValue(mid, out var mn) ? mn : "";
            items.Add(new EligibleBudgetDto
            {
                BudgetId = b.IdBudget,
                ExpenseHead = h?.HeadName ?? "",
                MainHead = mhName,
                CurrentAmount = b.BudgetAmount,
                RevisionsUsed = used,
                RevisionsRemaining = Math.Max(0, MaxPerHead - used),
                LastRevisedOn = lastOn == default ? null : lastOn
            });
        }
        return new EligibleBudgetListDto { Items = items, Total = items.Count };
    }

    public async Task<BudgetRevisionSummaryDto> GetSummaryAsync(int apartmentId, int fiscalYearId, CancellationToken cancellationToken = default)
    {
        var fy = await db.FiscalYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.IdFiscalYear == fiscalYearId, cancellationToken)
            .ConfigureAwait(false);
        var label = fy?.FyCode ?? $"FY{fiscalYearId}";
        var rows = await db.BudgetRevisions.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.FiscalYearId == fiscalYearId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var batches = rows.GroupBy(x => new { x.ApartmentId, x.FiscalYearId, x.CreatedBy, x.CreatedAt }).ToList();
        var dDraft = 0;
        var dP1 = 0;
        var dP2 = 0;
        var dAct = 0;
        var dRej = 0;
        foreach (var g in batches)
        {
            var st = NormalizeStatus(g.First().ApprovalStatus);
            switch (st)
            {
                case "Draft": dDraft++; break;
                case "PendingL1": dP1++; break;
                case "PendingL2": dP2++; break;
                case "Active": dAct++; break;
                case "Rejected": dRej++; break;
            }
        }
        var byStatus = new BudgetRevisionByStatusDto
            { Draft = dDraft, PendingL1 = dP1, PendingL2 = dP2, Active = dAct, Rejected = dRej };
        var lcA = 0;
        var lcP1 = 0;
        var lcP2 = 0;
        var lcR = 0;
        decimal deltaActive = 0, deltaPending = 0;
        foreach (var r in rows)
        {
            var st = NormalizeStatus(r.ApprovalStatus);
            var d = r.RevisedAmount - r.OriginalAmount;
            if (st == "Active") { lcA++; deltaActive += d; }
            else if (st == "PendingL1") { lcP1++; deltaPending += d; }
            else if (st == "PendingL2") { lcP2++; deltaPending += d; }
            else if (st == "Rejected") lcR++;
        }
        var lc = new BudgetRevisionLineCountsDto { Active = lcA, PendingL1 = lcP1, PendingL2 = lcP2, Rejected = lcR };
        return new BudgetRevisionSummaryDto
        {
            FiscalYearId = fiscalYearId,
            FiscalYearLabel = label,
            ByStatus = byStatus,
            LineCounts = lc,
            DeltaActiveTotal = deltaActive,
            DeltaPendingTotal = deltaPending
        };
    }

    private async Task<int> CountRevisionsForBudgetAsync(int apartmentId, int fiscalYearId, int budgetId, CancellationToken cancellationToken)
    {
        return await db.BudgetRevisions.AsNoTracking().CountAsync(x =>
            x.ApartmentId == apartmentId && x.FiscalYearId == fiscalYearId && x.BudgetId == budgetId &&
            (x.ApprovalStatus == null || x.ApprovalStatus == "" ||
             x.ApprovalStatus == "Draft" || x.ApprovalStatus == "PendingL1" || x.ApprovalStatus == "PendingL2" ||
             x.ApprovalStatus == "Active"), cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidateLineBudgetsAndQuotaAsync(
        int apartmentId,
        int fiscalYearId,
        List<BudgetRevisionLineInputDto> lines,
        IReadOnlyList<BudgetRevision>? batchBeingReplaced,
        CancellationToken cancellationToken)
    {
        var oldCounts = new Dictionary<int, int>();
        if (batchBeingReplaced is { Count: > 0 })
        {
            foreach (var g in batchBeingReplaced.GroupBy(x => x.BudgetId))
                oldCounts[g.Key] = g.Count();
        }
        foreach (var ln in lines.GroupBy(x => x.BudgetId))
        {
            var needed = ln.Count();
            var used = await CountRevisionsForBudgetAsync(apartmentId, fiscalYearId, ln.Key, cancellationToken).ConfigureAwait(false);
            var oldC = oldCounts.GetValueOrDefault(ln.Key);
            if (used - oldC + needed > MaxPerHead)
                throw new InvalidOperationException(
                    $"LIMIT_REACHED: Budget line {ln.Key} would exceed {MaxPerHead} revisions per FY for this head.");
        }
    }

    private async Task<Budget> RequireBudgetAsync(int apartmentId, int fiscalYearId, int budgetId, CancellationToken cancellationToken)
    {
        var b = await db.Budgets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.FiscalYearId == fiscalYearId && x.IdBudget == budgetId && x.IsActive,
                cancellationToken).ConfigureAwait(false);
        return b ?? throw new InvalidOperationException($"Budget {budgetId} not found for fiscal year.");
    }

    private async Task AssertAnnualBudgetActiveAsync(int apartmentId, int fiscalYearId, CancellationToken cancellationToken)
    {
        if (!await IsAnnualBudgetActiveAsync(apartmentId, fiscalYearId, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("CONFLICT: Annual budget must be approved (Active) before revisions.");
    }

    private async Task<bool> IsAnnualBudgetActiveAsync(int apartmentId, int fiscalYearId, CancellationToken cancellationToken)
    {
        var h = await db.BudgetHeaders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.FiscalYearId == fiscalYearId && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (h is null) return false;
        return h.ApprovedAt.HasValue ||
               string.Equals(h.StatusCode, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(h.StatusCode, "APPROVED", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(h.StatusCode, "LOCKED", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<BudgetRevision>> LoadBatchLinesAsync(int apartmentId, string batchId, CancellationToken cancellationToken)
    {
        if (!BudgetRevisionBatchId.TryDecode(batchId, out var apt, out var fy, out var usr, out var ts) || apt != apartmentId)
            return [];
        var tsLo = TruncateToSecondsUtc(ts);
        var tsHi = tsLo.AddSeconds(1);
        return await db.BudgetRevisions.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.FiscalYearId == fy && x.CreatedBy == usr &&
                        x.CreatedAt >= tsLo && x.CreatedAt < tsHi)
            .OrderBy(x => x.IdBudgetRevision)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<BudgetRevision>> LoadBatchLinesTrackedAsync(int apartmentId, string batchId, CancellationToken cancellationToken)
    {
        if (!BudgetRevisionBatchId.TryDecode(batchId, out var apt, out var fy, out var usr, out var ts) || apt != apartmentId)
            return [];
        var tsLo = TruncateToSecondsUtc(ts);
        var tsHi = tsLo.AddSeconds(1);
        return await db.BudgetRevisions
            .Where(x => x.ApartmentId == apartmentId && x.FiscalYearId == fy && x.CreatedBy == usr &&
                        x.CreatedAt >= tsLo && x.CreatedAt < tsHi)
            .OrderBy(x => x.IdBudgetRevision)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<BudgetRevisionBatchListItemDto> MapBatchListItemAsync(
        int apartmentId, List<BudgetRevision> g, CancellationToken cancellationToken)
    {
        var first = g[0];
        var batchId = BudgetRevisionBatchId.Encode(first.ApartmentId, first.FiscalYearId, first.CreatedBy, first.CreatedAt);
        var fy = await db.FiscalYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdFiscalYear == first.FiscalYearId, cancellationToken).ConfigureAwait(false);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.IdUser == first.CreatedBy, cancellationToken).ConfigureAwait(false);
        var role = await GetApartmentRoleCodeAsync(first.CreatedBy, apartmentId, cancellationToken).ConfigureAwait(false);
        var ver = await ComputeVersionLabelAsync(apartmentId, first.FiscalYearId, first.CreatedAt, cancellationToken).ConfigureAwait(false);
        var delta = g.Sum(x => x.RevisedAmount - x.OriginalAmount);
        return new BudgetRevisionBatchListItemDto
        {
            BatchId = batchId,
            Version = ver,
            Title = first.RevisionTitle ?? "",
            FiscalYearId = first.FiscalYearId,
            FiscalYearLabel = fy?.FyCode ?? $"FY{first.FiscalYearId}",
            LineCount = g.Count,
            DeltaTotal = delta,
            ApprovalStatus = NormalizeStatus(first.ApprovalStatus),
            IsLocked = string.Equals(first.IsLocked, "Y", StringComparison.OrdinalIgnoreCase),
            CreatedBy = new BudgetRevisionUserRefDto { UserId = first.CreatedBy, Name = user?.FullName ?? "", Role = role },
            CreatedAt = first.CreatedAt
        };
    }

    private async Task<string> ComputeVersionLabelAsync(
        int apartmentId, int fiscalYearId, DateTime createdAt, CancellationToken cancellationToken)
    {
        var keys = await db.BudgetRevisions.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && x.FiscalYearId == fiscalYearId && x.CreatedAt < createdAt)
            .Select(x => new { x.CreatedBy, x.CreatedAt })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var earlier = keys.DistinctBy(x => new { x.CreatedBy, Sec = TruncateToSecondsUtc(x.CreatedAt) }).Count();
        return $"v{earlier + 1}";
    }

    private static DateTime TruncateToSecondsUtc(DateTime dt)
    {
        var u = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
        return new DateTime(u.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }

    private async Task<BudgetRevisionBatchDetailDto> MapBatchDetailAsync(
        int apartmentId, List<BudgetRevision> lines, CancellationToken cancellationToken)
    {
        var first = lines[0];
        var batchId = BudgetRevisionBatchId.Encode(first.ApartmentId, first.FiscalYearId, first.CreatedBy, first.CreatedAt);
        var fy = await db.FiscalYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdFiscalYear == first.FiscalYearId, cancellationToken).ConfigureAwait(false);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.IdUser == first.CreatedBy, cancellationToken).ConfigureAwait(false);
        var role = await GetApartmentRoleCodeAsync(first.CreatedBy, apartmentId, cancellationToken).ConfigureAwait(false);
        var ver = await ComputeVersionLabelAsync(apartmentId, first.FiscalYearId, first.CreatedAt, cancellationToken).ConfigureAwait(false);
        var lineDtos = new List<BudgetRevisionLineDetailDto>();
        foreach (var line in lines)
            lineDtos.Add((await MapLineDetailAsync(line, batchId, cancellationToken).ConfigureAwait(false))!);
        var totals = new BudgetRevisionTotalsDto
        {
            OriginalTotal = lines.Sum(x => x.OriginalAmount),
            RevisedTotal = lines.Sum(x => x.RevisedAmount),
            DeltaTotal = lines.Sum(x => x.RevisedAmount - x.OriginalAmount)
        };
        var st = NormalizeStatus(first.ApprovalStatus);
        var approvalRef = new BudgetRevisionApprovalRefDto
        {
            ApprovalId = FormatApprovalRef(first),
            Approver = "BOTH",
            ApproverState = new BudgetRevisionApproverStateDto
            {
                Secretary = st == "PendingL1" ? "PENDING" : st is "PendingL2" or "Active" or "Rejected" ? "APPROVED" : "PENDING",
                President = st == "PendingL2" ? "PENDING" : st == "Active" ? "APPROVED" : st == "Rejected" ? "REJECTED" : "PENDING"
            }
        };
        return new BudgetRevisionBatchDetailDto
        {
            BatchId = batchId,
            Version = ver,
            Title = first.RevisionTitle ?? "",
            FiscalYearId = first.FiscalYearId,
            FiscalYearLabel = fy?.FyCode ?? $"FY{first.FiscalYearId}",
            OverallJustification = first.OverallJustification ?? "",
            SupportingDocUrl = first.SupportingDocUrl,
            ApprovalStatus = st,
            IsLocked = string.Equals(first.IsLocked, "Y", StringComparison.OrdinalIgnoreCase),
            CreatedBy = new BudgetRevisionUserRefDto { UserId = first.CreatedBy, Name = user?.FullName ?? "", Role = role },
            CreatedAt = first.CreatedAt,
            ModifiedAt = lines.Max(x => x.ModifiedAt),
            Lines = lineDtos,
            Totals = totals,
            ApprovalRef = approvalRef
        };
    }

    private async Task<BudgetRevisionLineDetailDto?> MapLineDetailAsync(BudgetRevision line, string batchId, CancellationToken cancellationToken)
    {
        var b = await db.Budgets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdBudget == line.BudgetId, cancellationToken).ConfigureAwait(false);
        var head = b is null
            ? null
            : await db.ExpenseHeads.AsNoTracking().FirstOrDefaultAsync(x => x.IdExpenseHead == b.ExpenseHeadId, cancellationToken)
                .ConfigureAwait(false);
        string mainName = "";
        if (head?.MainHeadId is { } mhId)
        {
            mainName = await db.ExpenseMainHeads.AsNoTracking().Where(x => x.IdExpenseMainHead == mhId).Select(x => x.MainHeadName)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? "";
        }
        return new BudgetRevisionLineDetailDto
        {
            IdBudgetRevision = line.IdBudgetRevision,
            BatchId = batchId,
            BudgetId = line.BudgetId,
            ExpenseHead = head?.HeadName ?? "",
            MainHead = mainName,
            OriginalAmount = line.OriginalAmount,
            RevisedAmount = line.RevisedAmount,
            Delta = line.RevisedAmount - line.OriginalAmount,
            ReasonForRevision = line.ReasonForRevision,
            ApprovalStatus = NormalizeStatus(line.ApprovalStatus),
            IsLocked = string.Equals(line.IsLocked, "Y", StringComparison.OrdinalIgnoreCase),
            ModifiedAt = line.ModifiedAt
        };
    }

    private static CreateBudgetRevisionBatchResultDto MapCreateResult(List<BudgetRevision> lines)
    {
        var first = lines[0];
        var batchId = BudgetRevisionBatchId.Encode(first.ApartmentId, first.FiscalYearId, first.CreatedBy, first.CreatedAt);
        var sums = lines.Select(x => new BudgetRevisionLineSummaryDto
        {
            IdBudgetRevision = x.IdBudgetRevision,
            BudgetId = x.BudgetId,
            OriginalAmount = x.OriginalAmount,
            RevisedAmount = x.RevisedAmount,
            Delta = x.RevisedAmount - x.OriginalAmount
        }).ToList();
        return new CreateBudgetRevisionBatchResultDto
        {
            BatchId = batchId,
            ApprovalStatus = "Draft",
            IsLocked = false,
            Lines = sums,
            Totals = new BudgetRevisionTotalsDto
            {
                OriginalTotal = lines.Sum(x => x.OriginalAmount),
                RevisedTotal = lines.Sum(x => x.RevisedAmount),
                DeltaTotal = lines.Sum(x => x.RevisedAmount - x.OriginalAmount)
            }
        };
    }

    private static string NormalizeStatus(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "Draft";
        return s.Trim();
    }

    private static string FormatApprovalRef(BudgetRevision first) => $"AR-{first.CreatedAt.Year}-{first.IdBudgetRevision:D4}";

    private static void ValidateCreateBody(CreateBudgetRevisionBatchRequest request)
    {
        if (request.Lines is null || request.Lines.Count == 0) throw new InvalidOperationException("lines are required.");
        var title = (request.RevisionTitle ?? "").Trim();
        if (title.Length is < 1 or > MaxTitle) throw new InvalidOperationException($"revisionTitle must be 1..{MaxTitle} characters.");
        request.RevisionTitle = title;
        var just = (request.OverallJustification ?? "").Trim();
        if (just.Length is < MinJustification or > MaxJustification)
            throw new InvalidOperationException($"overallJustification must be {MinJustification}..{MaxJustification} characters.");
        request.OverallJustification = just;
        foreach (var ln in request.Lines)
        {
            var r = (ln.ReasonForRevision ?? "").Trim();
            if (r.Length is < MinReason or > MaxReason) throw new InvalidOperationException($"reasonForRevision must be {MinReason}..{MaxReason} chars per line.");
            ln.ReasonForRevision = r;
        }
    }

    private static void ValidateUpdateBody(UpdateBudgetRevisionBatchRequest request)
    {
        if (request.Lines is null || request.Lines.Count == 0) throw new InvalidOperationException("lines are required.");
        var title = (request.RevisionTitle ?? "").Trim();
        if (title.Length is < 1 or > MaxTitle) throw new InvalidOperationException($"revisionTitle must be 1..{MaxTitle} characters.");
        request.RevisionTitle = title;
        var just = (request.OverallJustification ?? "").Trim();
        if (just.Length is < MinJustification or > MaxJustification)
            throw new InvalidOperationException($"overallJustification must be {MinJustification}..{MaxJustification} characters.");
        request.OverallJustification = just;
        foreach (var ln in request.Lines)
        {
            var r = (ln.ReasonForRevision ?? "").Trim();
            if (r.Length is < MinReason or > MaxReason) throw new InvalidOperationException($"reasonForRevision must be {MinReason}..{MaxReason} chars per line.");
            ln.ReasonForRevision = r;
        }
    }

    private async Task<string?> GetApartmentRoleCodeAsync(int userId, int apartmentId, CancellationToken cancellationToken)
    {
        var au = await db.ApartmentUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApartmentId == apartmentId && x.UserId == userId && x.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (au is null) return null;
        return await db.AppRoles.AsNoTracking().Where(x => x.IdRole == au.RoleId).Select(x => x.RoleCode)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsSecretary(string? roleCode) =>
        roleCode is not null &&
        (roleCode.Contains("SECRETARY", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(roleCode, "S", StringComparison.OrdinalIgnoreCase));

    private static bool IsPresident(string? roleCode) =>
        roleCode is not null &&
        (roleCode.Contains("PRESIDENT", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(roleCode, "P", StringComparison.OrdinalIgnoreCase));
}
