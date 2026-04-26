using Apartment_API.Models;

namespace Apartment_API.Database;

public static class InMemoryApartmentStore
{
    public static readonly List<Apartment> Apartments =
    [
        new Apartment
        {
            Title = "Studio apartment",
            Address = "Downtown, Block A",
            RentAmount = 15000m,
            IsAvailable = true
        },
        new Apartment
        {
            Title = "2 BHK apartment",
            Address = "Green Residency",
            RentAmount = 28000m,
            IsAvailable = false
        }
    ];
}
