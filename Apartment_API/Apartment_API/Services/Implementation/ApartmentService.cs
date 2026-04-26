using Apartment_API.Database;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Services.Interfaces;

namespace Apartment_API.Services.Implementation;

public sealed class ApartmentService(ILogger<ApartmentService> logger) : IApartmentService
{
    public Task<IReadOnlyCollection<ApartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Exceptions propagate to the controller (no catch here) unless you log and rethrow.
        logger.LogInformation("Loading apartments from in-memory store.");

        var items = InMemoryApartmentStore.Apartments
            .Select(a => a.ToDto())
            .ToArray();

        logger.LogInformation("Loaded {Count} apartment(s).", items.Length);
        return Task.FromResult<IReadOnlyCollection<ApartmentDto>>(items);
    }
}
