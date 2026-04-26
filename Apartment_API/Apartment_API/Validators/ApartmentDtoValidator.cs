using Apartment_API.DTO;

namespace Apartment_API.Validators;

public static class ApartmentDtoValidator
{
    public static bool IsValid(ApartmentDto dto) =>
        !string.IsNullOrWhiteSpace(dto.Title)
        && !string.IsNullOrWhiteSpace(dto.Address)
        && dto.RentAmount > 0;
}
