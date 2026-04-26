using Apartment_API.Database;
using Apartment_API.DTO;
using Apartment_API.Helpers;

namespace Apartment_API.Services;

public sealed class ApartmentService(ILogger<ApartmentService> logger) : IApartmentService
{
    public Task<IReadOnlyCollection<ApartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Loading apartments from in-memory store.");

            var items = InMemoryApartmentStore.Apartments
                .Select(a => a.ToDto())
                .ToArray();

            logger.LogInformation("Loaded {Count} apartment(s).", items.Length);
            return Task.FromResult<IReadOnlyCollection<ApartmentDto>>(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load apartments.");
            throw;
        }
    }
}
