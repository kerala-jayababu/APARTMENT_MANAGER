namespace Apartment_API.Configuration;

public sealed class OtpSettings
{
    public const string SectionName = "Otp";

    public int ExpiryMinutes { get; set; } = 10;
    public int CodeLength { get; set; } = 6;
}
