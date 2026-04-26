using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Services.Implementation;

public sealed class ApartmentService(AppDbContext db, ILogger<ApartmentService> logger) : IApartmentService
{
    public async Task<IReadOnlyCollection<ApartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await db.Apartments.AsNoTracking()
            .OrderBy(a => a.ApartmentName)
            .Select(a => new ApartmentDto
            {
                IdApartment = a.IdApartment,
                ApartmentCode = a.ApartmentCode,
                ApartmentName = a.ApartmentName,
                AssociationName = a.AssociationName,
                City = a.City,
                State = a.State,
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Loaded {Count} apartment(s) from database.", list.Count);
        return list;
    }
}
