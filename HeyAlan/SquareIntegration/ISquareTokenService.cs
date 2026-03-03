namespace HeyAlan.SquareIntegration;

public interface ISquareTokenService
{
    Task StoreConnectionAsync(SquareTokenStoreInput input, CancellationToken cancellationToken = default);

    Task<SquareTokenResolution> GetValidAccessTokenAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
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
