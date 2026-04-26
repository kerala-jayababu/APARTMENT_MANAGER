using Apartment_API.DTO;
using Apartment_API.Models;

namespace Apartment_API.Helpers;

public static class MappingExtensions
{
    public static ApartmentDto ToDto(this Apartment apartment) =>
        new()
        {
            Id = apartment.Id,
            Title = apartment.Title,
            Address = apartment.Address,
            RentAmount = apartment.RentAmount,
            IsAvailable = apartment.IsAvailable
        };
}
