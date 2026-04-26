using System.Security.Cryptography;
using Apartment_API.Configuration;
using Apartment_API.Data;
using Apartment_API.DTO;
using Apartment_API.Helpers;
using Apartment_API.Models;
using Apartment_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Apartment_API.Services.Implementation;

public sealed class OtpAuthService(
    AppDbContext db,
    IEmailService emailService,
    IOptions<OtpSettings> otpOptions,
    ILogger<OtpAuthService> logger) : IOtpAuthService
{
    private readonly OtpSettings _otp = otpOptions.Value;

    public async Task RequestLoginOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        email = email.Trim();
        if (string.IsNullOrEmpty(email))
            return;

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.IsActive)
        {
            logger.LogDebug("OTP request: no active user for {Email}.", email);
            return;
        }

        var length = Math.Clamp(_otp.CodeLength, 4, 9);
        var max = (int)Math.Pow(10, length);
        var code = RandomNumberGenerator.GetInt32(0, max).ToString($"D{length}");

        var expiry = DateTime.UtcNow.AddMinutes(Math.Max(1, _otp.ExpiryMinutes));
        user.LoginOTP = code;
        user.OTPCreatedDate = DateTime.UtcNow;
        user.OTPExpiryDate = expiry;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var subject = "Your login code";
        var body =
            $"""
             <p>Hi {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>
             <p>Your one-time login code is: <strong>{System.Net.WebUtility.HtmlEncode(code)}</strong></p>
             <p>This code expires at {expiry:u} (UTC).</p>
             <p>If you did not request this, you can ignore this email.</p>
             """;
        try
        {
            await emailService.SendHtmlAsync(user.Email, subject, body, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            user.LoginOTP = null;
            user.OTPCreatedDate = null;
            user.OTPExpiryDate = null;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<(UserPublicDto? user, string? errorCode)> VerifyLoginOtpAsync(
        string email, string otp, CancellationToken cancellationToken = default)
    {
        email = email.Trim();
        otp = (otp ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
            return (null, "INVALID_OTP");

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.IsActive)
            return (null, "INVALID_OTP");

        if (string.IsNullOrEmpty(user.LoginOTP) || user.OTPExpiryDate is null
            || user.OTPExpiryDate < DateTime.UtcNow)
            return (null, "EXPIRED_OR_INVALID_OTP");

        if (!string.Equals(user.LoginOTP, otp, StringComparison.Ordinal))
            return (null, "INVALID_OTP");

        user.LoginOTP = null;
        user.OTPCreatedDate = null;
        user.OTPExpiryDate = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (user.ToPublicDto(), null);
    }
}
