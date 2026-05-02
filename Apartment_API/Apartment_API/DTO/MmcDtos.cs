namespace Apartment_API.DTO;

public sealed class MmcPeriodDto
{
    public int Id { get; init; }
    public string PeriodCode { get; init; } = string.Empty;
    public string PeriodName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CreateMmcPeriodRequest
{
    public string PeriodCode { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public sealed class MmcGridLineDto
{
    public int UnitId { get; init; }
    public string? Block { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public string? PrimaryOwnerName { get; init; }
    public decimal CurrentMmcAmount { get; init; }
    public decimal? NewMmcAmount { get; init; }
    public bool HasPeriodRow { get; init; }
}

public sealed class MmcGridTotalsDto
{
    public decimal CurrentMonthlyTotal { get; init; }
    public decimal NewMonthlyTotal { get; init; }
    public decimal DeltaTotal { get; init; }
}

public sealed class MmcGridDto
{
    public int MmcPeriodId { get; init; }
    public string PeriodName { get; init; } = string.Empty;
    public IReadOnlyList<MmcGridLineDto> Lines { get; init; } = [];
    public MmcGridTotalsDto Totals { get; init; } = new();
}

public sealed class SubmitMmcBatchLineRequest
{
    public int UnitId { get; set; }
    public decimal NewMmcAmount { get; set; }
}

public sealed class SubmitMmcBatchRequest
{
    public int MmcPeriodId { get; set; }
    public string? Remarks { get; set; }
    public List<SubmitMmcBatchLineRequest> Lines { get; set; } = [];
}

public sealed class MmcBatchCreatedDto
{
    public int Id { get; init; }
    public int LineCount { get; init; }
}

public sealed class MmcBatchListDto
{
    public int Id { get; init; }
    public int MmcPeriodId { get; init; }
    public string PeriodName { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public int LineCount { get; init; }
    public decimal TotalNewMonthlyMmc { get; init; }
    public decimal TotalDelta { get; init; }
    public string? SubmittedByName { get; init; }
    public DateTime SubmittedAt { get; init; }
    public string? ApprovedByName { get; init; }
    public DateTime? ApprovedAt { get; init; }
}

public sealed class MmcBatchDetailLineDto
{
    public int UnitId { get; init; }
    public string? Block { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public string? PrimaryOwnerName { get; init; }
    public decimal PreviousMmcAmount { get; init; }
    public decimal NewMmcAmount { get; init; }
    public decimal DeltaAmount { get; init; }
}

public sealed class MmcBatchDetailTotalsDto
{
    public decimal PreviousMonthlyTotal { get; init; }
    public decimal NewMonthlyTotal { get; init; }
    public decimal DeltaTotal { get; init; }
}

public sealed class MmcBatchDetailDto
{
    public int Id { get; init; }
    public int MmcPeriodId { get; init; }
    public string PeriodName { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public string? Remarks { get; init; }
    public IReadOnlyList<MmcBatchDetailLineDto> Lines { get; init; } = [];
    public MmcBatchDetailTotalsDto Totals { get; init; } = new();
    public string? SubmittedByName { get; init; }
    public DateTime SubmittedAt { get; init; }
}

public sealed class UnitMmcHistoryDto
{
    public int IdUnitMmcDetail { get; init; }
    public int? MmcPeriodId { get; init; }
    public string? PeriodName { get; init; }
    public decimal MmcAmount { get; init; }
    public DateTime MmcPeriodFrom { get; init; }
    public DateTime? MmcPeriodTo { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedByName { get; init; }
    public bool IsActive { get; init; }
}
