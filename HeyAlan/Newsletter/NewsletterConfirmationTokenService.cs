namespace HeyAlan.Newsletter;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public sealed class NewsletterConfirmationTokenService 
    : INewsletterConfirmationTokenService
{
    private const string ProtectorPurpose = "newsletter-confirmation-v1";

    private readonly IDataProtector dataProtector;
    private readonly NewsletterOptions options;
    private readonly TimeProvider timeProvider;

    public NewsletterConfirmationTokenService(
        IDataProtectionProvider dataProtectionProvider,
        NewsletterOptions options,
        TimeProvider timeProvider)
    {
        if (dataProtectionProvider is null)
        {
            throw new ArgumentNullException(nameof(dataProtectionProvider));
        }

        this.dataProtector = dataProtectionProvider
            .CreateProtector(ProtectorPurpose);

        this.options = options ?? 
            throw new ArgumentNullException(nameof(options));

        this.timeProvider = timeProvider ??
            throw new ArgumentNullException(nameof(timeProvider));
    }

    public string CreateToken(string email, DateTimeOffset issuedAtUtc)
    {
        if (String.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }
                                                                                                                       
        string normalizedEmail = email.Trim();
        NewsletterConfirmationTokenPayload payload = new(
            normalizedEmail,
            issuedAtUtc.ToUniversalTime());

        string payloadJson = JsonSerializer.Serialize(payload);
        string protectedPayload = this.dataProtector.Protect(payloadJson);
        byte[] protectedPayloadBytes = Encoding.UTF8.GetBytes(protectedPayload);
        return WebEncoders.Base64UrlEncode(protectedPayloadBytes);
    }

    public bool TryReadEmail(string token, out string email)
    {
        email = String.Empty;

        if (String.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalizedToken = token.Trim();

        try
        {
            byte[] protectedPayloadBytes = WebEncoders.Base64UrlDecode(normalizedToken);
            string protectedPayload = Encoding.UTF8.GetString(protectedPayloadBytes);
            string payloadJson = this.dataProtector.Unprotect(protectedPayload);

            NewsletterConfirmationTokenPayload? payload = 
                JsonSerializer.Deserialize<NewsletterConfirmationTokenPayload>(payloadJson);

            if (payload is null || String.IsNullOrWhiteSpace(payload.Email))
            {
                return false;
            }

            DateTimeOffset nowUtc = this.timeProvider.GetUtcNow();

            DateTimeOffset expiresAtUtc = payload.IssuedAtUtc
                .AddMinutes(this.options.ConfirmTokenTtlMinutes);

            if (expiresAtUtc < nowUtc)
            {
                return false;
            }

            email = payload.Email.Trim();
            return email.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private sealed record NewsletterConfirmationTokenPayload(
        string Email,
        DateTimeOffset IssuedAtUtc);
}
