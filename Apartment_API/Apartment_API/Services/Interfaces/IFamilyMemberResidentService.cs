using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IFamilyMemberResidentService
{
    Task<PagedResult<FamilyMemberDto>> ListAsync(
        int apartmentId, int? unitId, int? parentPersonId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<FamilyMemberDto?> GetFamilyMemberAsync(int apartmentId, int personId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(
        int apartmentId, int userId, CreateFamilyMemberRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(
        int apartmentId, int userId, int personId, CreateFamilyMemberRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int apartmentId, int personId, CancellationToken cancellationToken = default);
}
