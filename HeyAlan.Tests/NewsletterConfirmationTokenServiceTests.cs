namespace HeyAlan.Tests;

using HeyAlan.Newsletter;
using Microsoft.AspNetCore.DataProtection;
using System.Text;

public class NewsletterConfirmationTokenServiceTests
{
    [Fact]
    public void CreateToken_ThenTryReadEmail_ReturnsEmail()
    {
        NewsletterConfirmationTokenService service = CreateService(DateTimeOffset.Parse("2026-03-05T10:00:00Z"), 60);

        string token = service.CreateToken("person@example.com", DateTimeOffset.Parse("2026-03-05T09:30:00Z"));

        bool isValid = service.TryReadEmail(token, out string email);
        Assert.True(isValid);
        Assert.Equal("person@example.com", email);
    }

    [Fact]
    public void TryReadEmail_WhenExpired_ReturnsFalse()
    {
        NewsletterConfirmationTokenService service = CreateService(DateTimeOffset.Parse("2026-03-05T12:00:00Z"), 60);

        string token = service.CreateToken("person@example.com", DateTimeOffset.Parse("2026-03-05T10:00:00Z"));

        bool isValid = service.TryReadEmail(token, out string email);
        Assert.False(isValid);
        Assert.Equal(String.Empty, email);
    }

    [Fact]
    public void TryReadEmail_WhenMalformedToken_ReturnsFalse()
    {
        NewsletterConfirmationTokenService service = CreateService(DateTimeOffset.Parse("2026-03-05T10:00:00Z"), 60);

        bool isValid = service.TryReadEmail("not-a-token", out string email);
        Assert.False(isValid);
        Assert.Equal(String.Empty, email);
    }

    private static NewsletterConfirmationTokenService CreateService(DateTimeOffset utcNow, int ttlMinutes)
    {
        FakeDataProtectionProvider dataProtectionProvider = new();
        SendGridOptions options = new()
        {
            ApiKey = "api-key",
            NewsletterListId = "list-id",
            ConfirmTokenTtlMinutes = ttlMinutes
        };

        return new NewsletterConfirmationTokenService(
            dataProtectionProvider,
            options,
            new FixedTimeProvider(utcNow));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return this.utcNow;
        }
    }

    private sealed class FakeDataProtectionProvider : IDataProtectionProvider, IDataProtector
    {
        private const string Prefix = "enc::";

        public IDataProtector CreateProtector(string purpose)
        {
            return this;
        }

        public string Protect(string plaintext)
        {
            return $"{Prefix}{plaintext}";
        }

        public string Unprotect(string protectedData)
        {
            if (!protectedData.StartsWith(Prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid protected payload.");
            }

            return protectedData.Substring(Prefix.Length);
        }

        public byte[] Protect(byte[] plaintext)
        {
            string raw = Convert.ToBase64String(plaintext);
            string wrapped = this.Protect(raw);
            return Encoding.UTF8.GetBytes(wrapped);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            string wrapped = Encoding.UTF8.GetString(protectedData);
            string raw = this.Unprotect(wrapped);
            return Convert.FromBase64String(raw);
        }
    }
}
