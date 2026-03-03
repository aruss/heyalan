namespace HeyAlan.SquareIntegration;

using HeyAlan.Configuration;
using Square;
using Square.OAuth;
using System.Text.Json;

public sealed class SquareOAuthClient : ISquareOAuthClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly AppOptions appOptions;

    public SquareOAuthClient(IHttpClientFactory httpClientFactory, AppOptions appOptions)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.appOptions = appOptions ?? throw new ArgumentNullException(nameof(appOptions));
    }

    public async Task<SquareTokenExchangeResult> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode))
        {
            return new SquareTokenExchangeResult.Failure("square_oauth_code_missing");
        }

        if (string.IsNullOrWhiteSpace(this.appOptions.SquareClientId) || string.IsNullOrWhiteSpace(this.appOptions.SquareClientSecret))
        {
            return new SquareTokenExchangeResult.Failure("square_not_configured");
        }

        SquareClient client = this.CreateAppClient();
        ObtainTokenRequest request = new()
        {
            ClientId = this.appOptions.SquareClientId,
            ClientSecret = this.appOptions.SquareClientSecret,
            Code = authorizationCode,
            RedirectUri = redirectUri,
            GrantType = "authorization_code"
        };

        try
        {
            ObtainTokenResponse response = await client.OAuth.ObtainTokenAsync(request, null, cancellationToken);
            string[] resolvedScopes = await this.ResolveScopesWithTokenStatusFallbackAsync(
                response,
                cancellationToken);

            SquareTokenExchangePayload? payload = ToTokenExchangePayload(response, resolvedScopes);
            if (payload is null)
            {
                return new SquareTokenExchangeResult.Failure("square_token_exchange_failed");
            }

            return new SquareTokenExchangeResult.Success(payload);
        }
        catch (SquareApiException)
        {
            return new SquareTokenExchangeResult.Failure("square_token_exchange_failed");
        }
    }

    public async Task<SquareRevokeResult> RevokeAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new SquareRevokeResult.InvalidOrRevoked();
        }

        if (string.IsNullOrWhiteSpace(this.appOptions.SquareClientId) || string.IsNullOrWhiteSpace(this.appOptions.SquareClientSecret))
        {
            return new SquareRevokeResult.Failure("square_not_configured");
        }

        SquareClient client = this.CreateAppClient();
        RevokeTokenRequest request = new()
        {
            AccessToken = accessToken,
            ClientId = this.appOptions.SquareClientId
        };

        RequestOptions requestOptions = new()
        {
            AdditionalHeaders = new Dictionary<string, string?>
            {
                ["Authorization"] = $"Client {this.appOptions.SquareClientSecret}"
            }
        };

        try
        {
            RevokeTokenResponse response = await client.OAuth.RevokeTokenAsync(request, requestOptions, cancellationToken);
            if (response.Success == true)
            {
                return new SquareRevokeResult.Success();
            }

            if (HasErrorCode(response.Errors, "ACCESS_TOKEN_REVOKED") ||
                HasErrorCode(response.Errors, "UNAUTHORIZED") ||
                HasErrorCode(response.Errors, "invalid_grant"))
            {
                return new SquareRevokeResult.InvalidOrRevoked();
            }

            if (response.Errors is null || !response.Errors.Any())
            {
                return new SquareRevokeResult.InvalidOrRevoked();
            }

            return new SquareRevokeResult.Failure("square_revoke_failed");
        }
        catch (SquareApiException exception)
        {
            if (exception.StatusCode is 400 or 401 ||
                HasErrorCode(exception.Errors, "ACCESS_TOKEN_REVOKED") ||
                HasErrorCode(exception.Errors, "UNAUTHORIZED") ||
                HasErrorCode(exception.Errors, "invalid_grant"))
            {
                return new SquareRevokeResult.InvalidOrRevoked();
            }

            return new SquareRevokeResult.Failure("square_revoke_failed");
        }
    }

    private SquareClient CreateAppClient()
    {
        string baseUrl = this.appOptions.SquareClientId!.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase)
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";

        ClientOptions options = new()
        {
            BaseUrl = baseUrl,
            HttpClient = this.httpClientFactory.CreateClient("SquareOAuthClient")
        };

        return new SquareClient(this.appOptions.SquareClientSecret!, clientOptions: options);
    }

    private SquareClient CreateAccessTokenClient(string accessToken)
    {
        string baseUrl = this.appOptions.SquareClientId!.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase)
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";

        ClientOptions options = new()
        {
            BaseUrl = baseUrl,
            HttpClient = this.httpClientFactory.CreateClient("SquareOAuthClient")
        };

        return new SquareClient(accessToken, clientOptions: options);
    }

    private async Task<string[]> ResolveScopesWithTokenStatusFallbackAsync(
        ObtainTokenResponse response,
        CancellationToken cancellationToken)
    {
        string[] scopesFromExchange = ResolveScopes(response);
        if (scopesFromExchange.Length > 0)
        {
            return scopesFromExchange;
        }

        if (string.IsNullOrWhiteSpace(response.AccessToken))
        {
            return [];
        }

        SquareClient accessTokenClient = this.CreateAccessTokenClient(response.AccessToken);
        try
        {
            RetrieveTokenStatusResponse tokenStatus = await accessTokenClient.OAuth.RetrieveTokenStatusAsync(null, cancellationToken);
            return NormalizeScopes(tokenStatus.Scopes);
        }
        catch (SquareApiException)
        {
            return [];
        }
    }

    private static SquareTokenExchangePayload? ToTokenExchangePayload(
        ObtainTokenResponse response,
        string[] scopes)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken) ||
            string.IsNullOrWhiteSpace(response.RefreshToken) ||
            string.IsNullOrWhiteSpace(response.MerchantId))
        {
            return null;
        }

        DateTime expiresAtUtc = ResolveAccessTokenExpiry(response.ExpiresAt);
        if (expiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        return new SquareTokenExchangePayload(
            response.AccessToken,
            response.RefreshToken,
            response.MerchantId,
            expiresAtUtc,
            scopes);
    }

    private static DateTime ResolveAccessTokenExpiry(object? expiresAtRaw)
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

    private static string[] ResolveScopes(ObtainTokenResponse response)
    {
        if (response.AdditionalProperties is null || response.AdditionalProperties.Count == 0)
        {
            return [];
        }

        if (response.AdditionalProperties.TryGetValue("scopes", out JsonElement scopesElement) &&
            scopesElement.ValueKind == JsonValueKind.Array)
        {
            return scopesElement
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!.Trim())
                .ToArray();
        }

        if (response.AdditionalProperties.TryGetValue("scope", out JsonElement scopeElement) &&
            scopeElement.ValueKind == JsonValueKind.String)
        {
            string? scopeRaw = scopeElement.GetString();
            if (!string.IsNullOrWhiteSpace(scopeRaw))
            {
                return scopeRaw
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        return [];
    }

    private static string[] NormalizeScopes(IEnumerable<string>? scopes)
    {
        if (scopes is null)
        {
            return [];
        }

        return scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasErrorCode(IEnumerable<Error>? errors, string expectedCode)
    {
        if (errors is null)
        {
            return false;
        }

        foreach (Error error in errors)
        {
            if (string.Equals(error.Code.ToString(), expectedCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
