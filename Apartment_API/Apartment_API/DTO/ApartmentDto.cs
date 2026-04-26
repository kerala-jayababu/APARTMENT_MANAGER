namespace Apartment_API.DTO;

/// <summary>Summary row for <c>dbo.Apartments</c> (list endpoint).</summary>
public sealed class ApartmentDto
{
    public int IdApartment { get; init; }
    public string ApartmentCode { get; init; } = string.Empty;
    public string ApartmentName { get; init; } = string.Empty;
    public string AssociationName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
