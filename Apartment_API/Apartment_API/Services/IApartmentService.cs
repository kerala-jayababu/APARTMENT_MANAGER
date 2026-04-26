using Apartment_API.DTO;

namespace Apartment_API.Services;

public interface IApartmentService
{
    Task<IReadOnlyCollection<ApartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
