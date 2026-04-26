using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IApartmentService
{
    Task<IReadOnlyCollection<ApartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
