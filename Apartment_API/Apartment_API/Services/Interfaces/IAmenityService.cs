using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IAmenityService
{
    Task<IReadOnlyList<AmenityListDto>> ListAsync(int apartmentId, bool? isActive, CancellationToken cancellationToken = default);
    Task<AmenityListDto?> GetByIdAsync(int apartmentId, int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(int apartmentId, int userId, CreateAmenityRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int apartmentId, int userId, int id, CreateAmenityRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AmenityBookingListDto>> ListBookingsAsync(
        int apartmentId,
        string? search,
        int? amenityId,
        int? statusId,
        CancellationToken cancellationToken = default);
    Task<int> CreateBookingAsync(
        int apartmentId,
        int userId,
        CreateAmenityBookingRequest request,
        CancellationToken cancellationToken = default);
    Task UpdateBookingAsync(
        int apartmentId,
        int userId,
        int id,
        CreateAmenityBookingRequest request,
        CancellationToken cancellationToken = default);
    Task CancelBookingAsync(
        int apartmentId,
        int userId,
        int id,
        CancelAmenityBookingRequest request,
        CancellationToken cancellationToken = default);
}
