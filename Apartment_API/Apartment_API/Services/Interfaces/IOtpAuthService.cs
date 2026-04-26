using Apartment_API.DTO;

namespace Apartment_API.Services.Interfaces;

public interface IOtpAuthService
{
    /// <summary>Generates OTP, stores on user, sends email. User-not-found is indistinguishable in response.</summary>
    Task RequestLoginOtpAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Validates OTP; returns public user for token creation, or an error code.</summary>
    Task<(UserPublicDto? user, string? errorCode)> VerifyLoginOtpAsync(
        string email, string otp, CancellationToken cancellationToken = default);
}
