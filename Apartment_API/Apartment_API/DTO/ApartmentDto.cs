namespace Apartment_API.DTO;

public sealed class ApartmentDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public decimal RentAmount { get; init; }
    public bool IsAvailable { get; init; }
}
