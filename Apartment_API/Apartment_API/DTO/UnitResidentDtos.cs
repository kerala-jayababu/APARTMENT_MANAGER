namespace Apartment_API.DTO;

// --- M02 units ---
public sealed class UnitListDto
{
    public int Id { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public string? Block { get; init; }
    public int Floor { get; init; }
    public string? UnitType { get; init; }
    public decimal? CarpetArea { get; init; }
    public string? Facing { get; init; }
    public string? UnitStatus { get; init; }
    public string? UnitStatusCode { get; init; }
    public string? PrimaryOwnerName { get; init; }
}

public sealed class PersonMiniDto
{
    public int PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
}

public sealed class CurrentMmcDto
{
    public decimal Amount { get; init; }
    public DateTime FromDate { get; init; }
}

public sealed class UnitDetailDto
{
    public int Id { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public int? BlockId { get; init; }
    public string? Block { get; init; }
    public int Floor { get; init; }
    public int UnitTypeId { get; init; }
    public string? UnitType { get; init; }
    public decimal? CarpetArea { get; init; }
    public decimal? BuiltUpArea { get; init; }
    public string? Facing { get; init; }
    public int UnitStatusId { get; init; }
    public string? UnitStatus { get; init; }
    public int OwnershipTypeId { get; init; }
    public string? OwnershipType { get; init; }
    public PersonMiniDto? PrimaryOwner { get; init; }
    public CurrentMmcDto? CurrentMmc { get; init; }
    public decimal OpeningReceivable { get; init; }
    public decimal OpeningAdvance { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CreateUnitRequest
{
    public string UnitNumber { get; set; } = string.Empty;
    public int? BlockId { get; set; }
    public int Floor { get; set; }
    public int UnitTypeId { get; set; }
    public decimal? CarpetArea { get; set; }
    public decimal? BuiltUpArea { get; set; }
    public string? Facing { get; set; }
    public int UnitStatusId { get; set; }
    public int OwnershipTypeId { get; set; }
    /// <summary>Existing Persons row (OWNER / CO-OWNER) selected from the Owner picker. Required when status implies ownership; co-owners are added later via POST /coowners.</summary>
    public int? PrimaryOwnerPersonId { get; set; }
    /// <summary>Defaults to today (UTC) when omitted.</summary>
    public DateTime? PrimaryOwnershipFromDate { get; set; }
    public decimal OpeningReceivable { get; set; }
    public decimal OpeningAdvance { get; set; }
}

public sealed class BulkGenerateUnitsRequest
{
    public int BlockId { get; set; }
    public int Floors { get; set; }
    public int UnitsPerFloor { get; set; }
    public int DefaultUnitTypeId { get; set; }
    public int DefaultCarpetArea { get; set; }
    public string UnitNumberPrefix { get; set; } = string.Empty;
    public int StartingFloor { get; set; } = 1;
    public int InitialUnitStatusId { get; set; }
}

public sealed class BulkGenerateResultDto
{
    public int CreatedCount { get; init; }
    public int SkippedExisting { get; init; }
}

public sealed class BlockOccupancyUnitDto
{
    public int UnitId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public int Floor { get; init; }
    public string? StatusCode { get; init; }
}

public sealed class BlockOccupancyDto
{
    public int BlockId { get; init; }
    public string BlockCode { get; init; } = string.Empty;
    public int TotalFloors { get; init; }
    public IReadOnlyList<BlockOccupancyUnitDto> Units { get; init; } = [];
}

public sealed class ChangeUnitStatusRequest
{
    public int NewStatusId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public int? LinkedPersonId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>POST /units/{id}/status — <see cref="CreatedNew"/> is false when the same request was applied again and the latest history row was refreshed.</summary>
public sealed class ChangeUnitStatusResultDto
{
    public int Id { get; init; }
    public bool CreatedNew { get; init; }
}

public sealed class IdCreatedDto
{
    public int Id { get; init; }
}

public sealed class UnitStatusHistoryDto
{
    public int Id { get; init; }
    public int UnitId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public string? Block { get; init; }
    public string? PreviousStatus { get; init; }
    public string? NewStatus { get; init; }
    public DateTime EffectiveDate { get; init; }
    public string? LinkedPersonName { get; init; }
    public string? LinkedPersonRole { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? ChangedByName { get; init; }
    public DateTime ChangedAt { get; init; }
}
