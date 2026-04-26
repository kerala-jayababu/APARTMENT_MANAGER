namespace Apartment_API.Models;

public class Apartment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal RentAmount { get; set; }
    public bool IsAvailable { get; set; } = true;
}
