namespace Apartment_API.DTO;

public sealed class BudgetLineDetailDto
{
    public int Id { get; init; }
    public int? MainHeadId { get; init; }
    public string? MainHeadName { get; init; }
    public int ExpenseHeadId { get; init; }
    public string ExpenseHeadCode { get; init; } = string.Empty;
    public string ExpenseHeadName { get; init; } = string.Empty;
    public decimal LastYrBudget { get; init; }
    public decimal LastYrActual { get; init; }
    public decimal BudgetAmount { get; init; }
    public decimal ActualAmount { get; init; }
    public string? Remarks { get; init; }
}

public sealed class BudgetTotalsDto
{
    public decimal LastYrBudget { get; init; }
    public decimal LastYrActual { get; init; }
    public decimal BudgetAmount { get; init; }
    public decimal ActualAmount { get; init; }
}

public sealed class BudgetDetailDto
{
    public int FiscalYearId { get; init; }
    public string FyCode { get; init; } = string.Empty;
    public string Status { get; init; } = "DRAFT";
    public DateTime? SubmittedAt { get; init; }
    public string? SubmittedByName { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? ApprovedByName { get; init; }
    public IReadOnlyList<BudgetLineDetailDto> Lines { get; init; } = [];
    public BudgetTotalsDto Totals { get; init; } = new();
}

public sealed class SaveBudgetLineRequest
{
    public int? Id { get; set; }
    public int ExpenseHeadId { get; set; }
    public decimal BudgetAmount { get; set; }
    public string? Remarks { get; set; }
}

public sealed class SaveBudgetRequest
{
    public List<SaveBudgetLineRequest> Lines { get; set; } = [];
}
