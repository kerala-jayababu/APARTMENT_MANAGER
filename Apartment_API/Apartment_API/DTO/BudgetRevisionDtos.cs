namespace Apartment_API.DTO;

public sealed class BudgetRevisionUserRefDto
{
    public int UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Role { get; init; }
}

public sealed class BudgetRevisionLineInputDto
{
    public int BudgetId { get; set; }
    public decimal RevisedAmount { get; set; }
    public string ReasonForRevision { get; set; } = string.Empty;
}

public sealed class CreateBudgetRevisionBatchRequest
{
    public int FiscalYearId { get; set; }
    public string RevisionTitle { get; set; } = string.Empty;
    public string OverallJustification { get; set; } = string.Empty;
    public string? SupportingDocUrl { get; set; }
    public List<BudgetRevisionLineInputDto> Lines { get; set; } = [];
}

public sealed class UpdateBudgetRevisionBatchRequest
{
    public string RevisionTitle { get; set; } = string.Empty;
    public string OverallJustification { get; set; } = string.Empty;
    public string? SupportingDocUrl { get; set; }
    public List<BudgetRevisionLineInputDto> Lines { get; set; } = [];
}

public sealed class PatchBudgetRevisionLineRequest
{
    public decimal? RevisedAmount { get; set; }
    public string? ReasonForRevision { get; set; }
}

public sealed class BudgetRevisionLineSummaryDto
{
    public int IdBudgetRevision { get; init; }
    public int BudgetId { get; init; }
    public decimal OriginalAmount { get; init; }
    public decimal RevisedAmount { get; init; }
    public decimal Delta { get; init; }
}

public sealed class BudgetRevisionTotalsDto
{
    public decimal OriginalTotal { get; init; }
    public decimal RevisedTotal { get; init; }
    public decimal DeltaTotal { get; init; }
}

public sealed class CreateBudgetRevisionBatchResultDto
{
    public string BatchId { get; init; } = string.Empty;
    public string ApprovalStatus { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public IReadOnlyList<BudgetRevisionLineSummaryDto> Lines { get; init; } = [];
    public BudgetRevisionTotalsDto Totals { get; init; } = new();
}

public sealed class BudgetRevisionBatchListItemDto
{
    public string BatchId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int FiscalYearId { get; init; }
    public string FiscalYearLabel { get; init; } = string.Empty;
    public int LineCount { get; init; }
    public decimal DeltaTotal { get; init; }
    public string ApprovalStatus { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public BudgetRevisionUserRefDto CreatedBy { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public sealed class BudgetRevisionLineDetailDto
{
    public int IdBudgetRevision { get; init; }
    public string BatchId { get; init; } = string.Empty;
    public int BudgetId { get; init; }
    public string ExpenseHead { get; init; } = string.Empty;
    public string MainHead { get; init; } = string.Empty;
    public decimal OriginalAmount { get; init; }
    public decimal RevisedAmount { get; init; }
    public decimal Delta { get; init; }
    public string ReasonForRevision { get; init; } = string.Empty;
    public string ApprovalStatus { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

public sealed class BudgetRevisionApprovalRefDto
{
    public string ApprovalId { get; init; } = string.Empty;
    public string Approver { get; init; } = string.Empty;
    public BudgetRevisionApproverStateDto ApproverState { get; init; } = new();
}

public sealed class BudgetRevisionApproverStateDto
{
    public string Secretary { get; init; } = "PENDING";
    public string President { get; init; } = "PENDING";
}

public sealed class BudgetRevisionBatchDetailDto
{
    public string BatchId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int FiscalYearId { get; init; }
    public string FiscalYearLabel { get; init; } = string.Empty;
    public string OverallJustification { get; init; } = string.Empty;
    public string? SupportingDocUrl { get; init; }
    public string ApprovalStatus { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public BudgetRevisionUserRefDto CreatedBy { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public IReadOnlyList<BudgetRevisionLineDetailDto> Lines { get; init; } = [];
    public BudgetRevisionTotalsDto Totals { get; init; } = new();
    public BudgetRevisionApprovalRefDto? ApprovalRef { get; init; }
}

public sealed class BudgetRevisionSubmitRequest
{
    public string? Note { get; set; }
}

public sealed class BudgetRevisionSubmitResultDto
{
    public string BatchId { get; init; } = string.Empty;
    public string ApprovalStatus { get; init; } = string.Empty;
    public BudgetRevisionApprovalRefLiteDto? ApprovalRef { get; init; }
    public DateTime SubmittedAt { get; init; }
}

public sealed class BudgetRevisionApprovalRefLiteDto
{
    public string ApprovalId { get; init; } = string.Empty;
}

public sealed class BudgetRevisionRecallRequest
{
    public string? Reason { get; set; }
}

public sealed class BudgetRevisionApproveRequest
{
    public string? Level { get; set; }
    public string? Note { get; set; }
}

public sealed class BudgetRevisionApproveResultDto
{
    public string BatchId { get; init; } = string.Empty;
    public string ApprovalStatus { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public DateTime? L1ApprovedAt { get; init; }
    public DateTime? L2ApprovedAt { get; init; }
    public int LockedRows { get; init; }
    public IReadOnlyList<BudgetUpdatedDto> BudgetsUpdated { get; init; } = [];
}

public sealed class BudgetUpdatedDto
{
    public int BudgetId { get; init; }
    public decimal FromAmount { get; init; }
    public decimal ToAmount { get; init; }
}

public sealed class BudgetRevisionRejectRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class BudgetRevisionRejectResultDto
{
    public string BatchId { get; init; } = string.Empty;
    public string ApprovalStatus { get; init; } = string.Empty;
    public DateTime? RejectedAt { get; init; }
}

public sealed class BudgetRevisionFileUploadDto
{
    public string Url { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Mime { get; init; } = string.Empty;
}

public sealed class EligibleBudgetDto
{
    public int BudgetId { get; init; }
    public string ExpenseHead { get; init; } = string.Empty;
    public string MainHead { get; init; } = string.Empty;
    public decimal CurrentAmount { get; init; }
    public int RevisionsUsed { get; init; }
    public int RevisionsRemaining { get; init; }
    public DateTime? LastRevisedOn { get; init; }
}

public sealed class EligibleBudgetListDto
{
    public IReadOnlyList<EligibleBudgetDto> Items { get; init; } = [];
    public int Total { get; init; }
}

public sealed class BudgetRevisionSummaryDto
{
    public int FiscalYearId { get; init; }
    public string FiscalYearLabel { get; init; } = string.Empty;
    public BudgetRevisionByStatusDto ByStatus { get; init; } = new();
    public BudgetRevisionLineCountsDto LineCounts { get; init; } = new();
    public decimal DeltaActiveTotal { get; init; }
    public decimal DeltaPendingTotal { get; init; }
}

public sealed class BudgetRevisionByStatusDto
{
    public int Draft { get; init; }
    public int PendingL1 { get; init; }
    public int PendingL2 { get; init; }
    public int Active { get; init; }
    public int Rejected { get; init; }
}

public sealed class BudgetRevisionLineCountsDto
{
    public int Active { get; init; }
    public int PendingL1 { get; init; }
    public int PendingL2 { get; init; }
    public int Rejected { get; init; }
}
