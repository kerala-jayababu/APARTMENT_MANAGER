namespace Apartment_API.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 60;

    /// <summary>Short-lived token used only to list apartments and request the access token (not the API access JWT).</summary>
    public int ApartmentSelectionExpiresInMinutes { get; set; } = 15;
}
