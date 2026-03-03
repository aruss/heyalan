namespace HeyAlan.SquareIntegration;

public sealed record SquareTokenExchangePayload(
    string AccessToken,
    string RefreshToken,
    string MerchantId,
    DateTime AccessTokenExpiresAtUtc,
    string[] Scopes);

public abstract record SquareTokenExchangeResult
{
    public sealed record Success(SquareTokenExchangePayload Payload) : SquareTokenExchangeResult;

    public sealed record Failure(string ErrorCode) : SquareTokenExchangeResult;
}

public abstract record SquareRevokeResult
{
    public sealed record Success : SquareRevokeResult;

    public sealed record InvalidOrRevoked : SquareRevokeResult;

    public sealed record Failure(string ErrorCode) : SquareRevokeResult;
}

public interface ISquareOAuthClient
{
    Task<SquareTokenExchangeResult> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<SquareRevokeResult> RevokeAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}
