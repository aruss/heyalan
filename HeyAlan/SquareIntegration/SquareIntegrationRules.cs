namespace HeyAlan.SquareIntegration;

public static class SquareIntegrationRules
{
    public const string ConnectCallbackPath = "/api/subscriptions/square/callback";

    private static readonly string[] RequiredFullScopes =
    [
        "ITEMS_READ",
        "CUSTOMERS_READ",
        "CUSTOMERS_WRITE",
        "ORDERS_READ",
        "ORDERS_WRITE",
        "PAYMENTS_WRITE"
    ];

    public static IReadOnlyCollection<string> GetRequiredScopes()
    {
        return RequiredFullScopes;
    }

    public static string ResolveOAuthBaseUrl(string clientId)
    {
        return IsSandboxClientId(clientId)
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";
    }

    public static string ResolveAuthorizeUrl(string clientId)
    {
        string oauthBaseUrl = ResolveOAuthBaseUrl(clientId);
        return $"{oauthBaseUrl}/oauth2/authorize";
    }

    public static bool IsSandboxClientId(string clientId)
    {
        return clientId.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase);
    }

    public static string[] NormalizeScopes(IEnumerable<string>? scopes)
    {
        if (scopes is null)
        {
            return [];
        }

        string[] normalizedScopes = scopes
            .Where(scope => !String.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(scope => scope, StringComparer.Ordinal)
            .ToArray();

        return normalizedScopes;
    }

    public static string NormalizeScopesForStorage(IEnumerable<string>? scopes)
    {
        string[] normalizedScopes = NormalizeScopes(scopes);
        return String.Join(' ', normalizedScopes);
    }

    public static string[] ParseStoredScopes(string? scopes)
    {
        if (String.IsNullOrWhiteSpace(scopes))
        {
            return [];
        }

        return scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static bool HasRequiredScopes(IEnumerable<string>? scopes)
    {
        HashSet<string> grantedScopes = NormalizeScopes(scopes).ToHashSet(StringComparer.Ordinal);
        return RequiredFullScopes.All(grantedScopes.Contains);
    }

    public static DateTime ResolveAccessTokenExpiry(object? expiresAtRaw)
    {
        if (expiresAtRaw is null)
        {
            return DateTime.UtcNow.AddMinutes(-1);
        }

        if (expiresAtRaw is DateTime expiresAtDateTime)
        {
            return expiresAtDateTime.Kind == DateTimeKind.Utc
                ? expiresAtDateTime
                : expiresAtDateTime.ToUniversalTime();
        }

        if (expiresAtRaw is DateTimeOffset expiresAtOffset)
        {
            return expiresAtOffset.UtcDateTime;
        }

        if (DateTimeOffset.TryParse(expiresAtRaw.ToString(), out DateTimeOffset parsedOffset))
        {
            return parsedOffset.UtcDateTime;
        }

        return DateTime.UtcNow.AddMinutes(-1);
    }
}
