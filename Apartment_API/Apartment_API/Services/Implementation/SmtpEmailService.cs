using System.Net;
using System.Net.Mail;
using Apartment_API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Apartment_API.Services.Implementation;

/// <summary>Loads SMTP values only from <c>appsettings.json</c> in the app content root (not from env vars, user-secrets, or <c>appsettings.Development.json</c>).</summary>
public sealed class SmtpEmailService(IHostEnvironment hostEnvironment, ILogger<SmtpEmailService> logger) : IEmailService
{
    private const string FileName = "appsettings.json";
    private const string SectionKey = "EmailSettings";

    private IConfigurationSection Section { get; } = new ConfigurationBuilder()
        .SetBasePath(hostEnvironment.ContentRootPath)
        .AddJsonFile(FileName, optional: false, reloadOnChange: true)
        .Build()
        .GetSection(SectionKey);

    public async Task SendHtmlAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var s = Section;
        var email = s["Email"] ?? string.Empty;
        var password = s["Password"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "EmailSettings: email or password is not configured in appsettings.json (root file only).");

        var host = s["SmtpHost"] ?? "smtp.gmail.com";
        var port = s.GetValue("SmtpPort", 587);
        var enableSsl = s.GetValue("EnableSsl", true);
        var displayName = s["DisplayName"];

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(email, password)
        };

        var fromName = displayName ?? email;
        using var message = new MailMessage
        {
            From = new MailAddress(email, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);
        await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Email sent to {To} with subject {Subject}.", toEmail, subject);
    }
}
