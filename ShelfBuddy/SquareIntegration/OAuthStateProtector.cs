namespace ShelfBuddy.SquareIntegration;

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

public sealed class OAuthStateProtector : IOAuthStateProtector
{
    private const string DataProtectionPurpose = "SquareIntegration.ConnectState.v1";
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(1);

    private readonly IDataProtector dataProtector;

    public OAuthStateProtector(IDataProtectionProvider dataProtectionProvider)
    {
        this.dataProtector = (dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider)))
            .CreateProtector(DataProtectionPurpose);
    }

    public string Protect(SquareConnectStatePayload payload)
    {
        string serializedPayload = JsonSerializer.Serialize(payload);
        string protectedPayload = this.dataProtector.Protect(serializedPayload);
        return protectedPayload;
    }

    public bool TryUnprotect(string protectedState, out SquareConnectStatePayload? payload)
    {
        payload = null;

        if (string.IsNullOrWhiteSpace(protectedState))
        {
            return false;
        }

        try
        {
            string rawPayload = this.dataProtector.Unprotect(protectedState);
            SquareConnectStatePayload? parsedPayload = JsonSerializer.Deserialize<SquareConnectStatePayload>(rawPayload);
            if (parsedPayload is null)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;
            if (parsedPayload.IssuedAtUtc > now + MaxClockSkew)
            {
                return false;
            }

            if (now - parsedPayload.IssuedAtUtc > MaxAge)
            {
                return false;
            }

            payload = parsedPayload;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
