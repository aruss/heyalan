namespace HeyAlan.SquareIntegration;

public sealed record StartSquareConnectInput(
    Guid SubscriptionId,
    Guid UserId,
    string ReturnUrl);

public abstract record StartSquareConnectResult
{
    public sealed record Success(string AuthorizeUrl) : StartSquareConnectResult;

    public sealed record Failure(string ErrorCode) : StartSquareConnectResult;
}

public sealed record CompleteSquareConnectInput(
    string? State,
    string? AuthorizationCode,
    string? OAuthError);

public abstract record CompleteSquareConnectResult
{
    public sealed record Success(string RedirectUrl) : CompleteSquareConnectResult;

    public sealed record Failure(string RedirectUrl, string ErrorCode) : CompleteSquareConnectResult;
}

public sealed record DisconnectSquareConnectionInput(
    Guid SubscriptionId,
    Guid UserId);

public abstract record DisconnectSquareConnectionResult
{
    public sealed record Success : DisconnectSquareConnectionResult;

    public sealed record Failure(string ErrorCode) : DisconnectSquareConnectionResult;
}

public sealed record SquareTokenStoreInput(
    Guid SubscriptionId,
    Guid ConnectedByUserId,
    string MerchantId,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    IReadOnlyCollection<string> Scopes);

public abstract record SquareTokenResolution
{
    public sealed record Success(string AccessToken, DateTime AccessTokenExpiresAtUtc) : SquareTokenResolution;

    public sealed record ConnectionMissing : SquareTokenResolution;

    public sealed record ReconnectRequired(string ReasonCode) : SquareTokenResolution;

    public sealed record RefreshFailed(string ReasonCode) : SquareTokenResolution;
}

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

public interface ISquareService
{
    Task<StartSquareConnectResult> StartConnectAsync(
        StartSquareConnectInput input,
        CancellationToken cancellationToken = default);

    Task<CompleteSquareConnectResult> CompleteConnectAsync(
        CompleteSquareConnectInput input,
        CancellationToken cancellationToken = default);

    Task<DisconnectSquareConnectionResult> DisconnectAsync(
        DisconnectSquareConnectionInput input,
        CancellationToken cancellationToken = default);

    Task StoreConnectionAsync(
        SquareTokenStoreInput input,
        CancellationToken cancellationToken = default);

    Task<SquareTokenResolution> GetValidAccessTokenAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<SquareTokenExchangeResult> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<SquareRevokeResult> RevokeAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}
