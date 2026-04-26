using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Returns (user, null) on success, or (null, error code) for expected business cases.
    /// Database/connection errors are not caught here: they throw and are handled in the controller.
    /// </summary>
    Task<(UserPublicDto user, string? errorCode)> RegisterAsync(
        RegisterRequestDto request, CancellationToken cancellationToken = default);

    Task<(UserPublicDto user, string? errorCode)> LoginAsync(
        LoginRequestDto request, CancellationToken cancellationToken = default);
}
