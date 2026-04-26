using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IOwnershipTransferResidentService
{
    Task<OwnershipTransferCreatedDto> RecordAsync(
        int apartmentId, int userId, RecordOwnershipTransferRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnershipHistoryItemDto>> GetHistoryForUnitAsync(
        int apartmentId, int unitId, CancellationToken cancellationToken = default);
    Task<IdProofResultDto> UploadDeedAsync(
        int apartmentId, int userId, int idOwnershipHistory, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
