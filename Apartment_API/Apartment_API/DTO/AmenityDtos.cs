namespace Apartment_API.DTO;

public sealed class AmenityListDto
{
    public int Id { get; init; }
    public int AmenityTypeId { get; init; }
    public string AmenityTypeName { get; init; } = string.Empty;
    public int IncomeHeadId { get; init; }
    public string IncomeHeadName { get; init; } = string.Empty;
    public string AmenityName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public short? Capacity { get; init; }
    public int ChargeTypeId { get; init; }
    public string ChargeTypeName { get; init; } = string.Empty;
    public decimal ChargeAmount { get; init; }
    public byte AdvanceBookingDays { get; init; }
    public int MaxContinuousSessionsAllowed { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CreateAmenityRequest
{
    public int AmenityTypeId { get; set; }
    public int IncomeHeadId { get; set; }
    public string AmenityName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short? Capacity { get; set; }
    public int ChargeTypeId { get; set; }
    public decimal ChargeAmount { get; set; }
    public byte AdvanceBookingDays { get; set; }
    public int MaxContinuousSessionsAllowed { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AmenityBookingListDto
{
    public int Id { get; init; }
    public string BookingCode { get; init; } = string.Empty;
    public int UnitId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public int PersonId { get; init; }
    public string? OwnerName { get; init; }
    public string? OwnerPhone { get; init; }
    public int AmenityId { get; init; }
    public string AmenityName { get; init; } = string.Empty;
    public DateTime BookingDate { get; init; }
    public TimeSpan SlotStartTime { get; init; }
    public TimeSpan SlotEndTime { get; init; }
    public decimal ChargeAmount { get; init; }
    public int? InvoiceId { get; init; }
    public int? StatusId { get; init; }
    public string? StatusName { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CreateAmenityBookingRequest
{
    public int AmenityId { get; set; }
    public int UnitId { get; set; }
    public int PersonId { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan SlotStartTime { get; set; }
    public TimeSpan SlotEndTime { get; set; }
    public decimal ChargeAmount { get; set; }
    public int? StatusId { get; set; }
    public string? Notes { get; set; }
}

public sealed class CancelAmenityBookingRequest
{
    public string? Reason { get; set; }
}
