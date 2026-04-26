namespace Apartment_API.DTO;

public sealed class BlockListDto
{
    public int Id { get; init; }
    public string BlockCode { get; init; } = string.Empty;
    public string BlockName { get; init; } = string.Empty;
    public int TotalFloors { get; init; }
    public int TotalUnits { get; init; }
    public int OccupiedUnits { get; init; }
    public int VacantUnits { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CreateBlockRequest
{
    public string BlockCode { get; set; } = string.Empty;
    public string BlockName { get; set; } = string.Empty;
    public int TotalFloors { get; set; }
    public int TotalUnits { get; set; }
    public string? Description { get; set; }
}

public sealed class IdResultDto
{
    public int Id { get; init; }
}
