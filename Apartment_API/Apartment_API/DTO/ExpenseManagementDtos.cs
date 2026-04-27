namespace Apartment_API.DTO;

public sealed class ExpenseSummaryDto
{
    public int TotalBills { get; init; }
    public decimal TotalAmount { get; init; }
    public int PendingApprovalCount { get; init; }
    public decimal PendingApprovalAmount { get; init; }
    public int ApprovedUnpaidCount { get; init; }
    public decimal ApprovedUnpaidAmount { get; init; }
    public decimal PaidThisMonth { get; init; }
    public string Period { get; init; } = string.Empty;
}

public sealed class ExpenseBillListItemDto
{
    public long BillId { get; init; }
    public string BillNumber { get; init; } = string.Empty;
    public DateTime BillDate { get; init; }
    public DateTime? DueDate { get; init; }
    public int VendorId { get; init; }
    public string VendorName { get; init; } = string.Empty;
    public int ExpenseHeadId { get; init; }
    public string ExpenseHeadName { get; init; } = string.Empty;
    public decimal GrossAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TdsAmount { get; init; }
    public decimal NetPayable { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal OutstandingAmount { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public string ApprovalTier { get; init; } = string.Empty;
    public bool HasJv { get; init; }
    public string? JvReference { get; init; }
    public string PaymentStatusLabel { get; init; } = string.Empty;
    public bool IsMigrated { get; init; }
}

public sealed class ExpenseBillLineInputDto
{
    public int ExpenseHeadId { get; set; }
    public bool IsAsset { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? HsnSacCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal GstPercentage { get; set; }
}

public sealed class ExpensePaymentInputDto
{
    public DateTime PaymentDate { get; set; }
    public int BankAccountId { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public decimal AmountPaid { get; set; }
    public bool IsFullPayment { get; set; }
}

public sealed class CreateExpenseBillRequest
{
    public string BillType { get; set; } = "REGULAR";
    public int VendorId { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? PaymentTerms { get; set; }
    public string? ExpenseCategory { get; set; }
    public string? FundSource { get; set; }
    public int BudgetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TdsAmount { get; set; }
    public decimal RetentionHeld { get; set; }
    public decimal RoundOff { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentBase64 { get; set; }
    public string Action { get; set; } = "DRAFT";
    public bool PaymentAlreadyMade { get; set; }
    public IReadOnlyList<ExpenseBillLineInputDto> Lines { get; set; } = [];
    public ExpensePaymentInputDto? Payment { get; set; }
}

public sealed class CreateContractExpenseRequest
{
    public int VendorId { get; set; }
    public string ContractReference { get; set; } = string.Empty;
    public DateTime ContractStartDate { get; set; }
    public DateTime ContractEndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int ExpenseHeadId { get; set; }
    public bool IsAsset { get; set; }
    public string? HsnSacCode { get; set; }
    public decimal TotalContractValue { get; set; }
    public decimal GstPercentage { get; set; }
    public decimal TdsAmount { get; set; }
    public string? FundSource { get; set; }
    public int BudgetId { get; set; }
    public string AccrualFrequency { get; set; } = "MONTHLY";
    public string PaymentFrequency { get; set; } = "MONTHLY";
    public DateTime FirstPaymentDue { get; set; }
    public int BankAccountId { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string? AttachmentFileName { get; set; }
    public string? AttachmentBase64 { get; set; }
    public string Action { get; set; } = "SUBMIT";
    public bool AdvanceAlreadyPaid { get; set; }
    public ExpensePaymentInputDto? AdvancePayment { get; set; }
}

public sealed class ExpenseBillUpsertResponseDto
{
    public long BillId { get; init; }
    public string BillNumber { get; init; } = string.Empty;
    public decimal NetPayable { get; init; }
    public string ApprovalTier { get; init; } = string.Empty;
    public string CurrentStatus { get; init; } = string.Empty;
    public string CurrentStatusName { get; init; } = string.Empty;
    public string? JvReference { get; init; }
    public string? BudgetWarning { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ExpenseLineComputedDto
{
    public decimal TaxableAmount { get; init; }
    public decimal GstAmount { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class ExpenseCalculateRequest
{
    public decimal TdsAmount { get; set; }
    public decimal RetentionHeld { get; set; }
    public decimal RoundOff { get; set; }
    public IReadOnlyList<ExpenseBillLineInputDto> Lines { get; set; } = [];
}

public sealed class ExpenseCalculateResponseDto
{
    public IReadOnlyList<ExpenseLineComputedDto> Lines { get; init; } = [];
    public decimal SubTotalTaxable { get; init; }
    public decimal TotalGst { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TdsAmount { get; init; }
    public decimal RetentionHeld { get; init; }
    public decimal RoundOff { get; init; }
    public decimal NetPayable { get; init; }
    public string ApprovalTier { get; init; } = string.Empty;
    public string ApprovalTierLabel { get; init; } = string.Empty;
    public string? BudgetUtilisationWarning { get; init; }
}

public sealed class ExpenseBudgetCheckDto
{
    public int ExpenseHeadId { get; init; }
    public string ExpenseHeadName { get; init; } = string.Empty;
    public decimal BudgetAmount { get; init; }
    public decimal ActualSpent { get; init; }
    public decimal CommittedAmount { get; init; }
    public decimal BudgetRemaining { get; init; }
    public decimal UtilisationPercent { get; init; }
    public decimal ProjectedUtilisationPercent { get; init; }
    public bool IsOverBudget { get; init; }
    public bool IsNearThreshold { get; init; }
    public string? WarningMessage { get; init; }
}

public sealed class ExpenseBillDeleteResponseDto
{
    public long BillId { get; init; }
    public bool Deleted { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ExpenseSimpleLookupDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? GstNumber { get; init; }
    public string? GlCode { get; init; }
    public string? FundSource { get; init; }
    public string? AccountName { get; init; }
    public string? BankName { get; init; }
    public string? AccountNumberMasked { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool? IsCurrent { get; init; }
    public string? Label { get; init; }
}
