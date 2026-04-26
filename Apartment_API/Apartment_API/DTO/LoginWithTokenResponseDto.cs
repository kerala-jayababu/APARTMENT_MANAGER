namespace Apartment_API.DTO;

public sealed class LoginWithTokenResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTime ExpiresAtUtc { get; init; }
    public UserPublicDto User { get; init; } = null!;
}
