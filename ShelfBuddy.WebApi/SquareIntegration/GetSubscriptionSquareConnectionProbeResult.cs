namespace ShelfBuddy.WebApi.SquareIntegration;

public record GetSubscriptionSquareConnectionProbeResult(
    bool IsConnected,
    string MerchantId,
    DateTime AccessTokenExpiresAtUtc,
    string[] Scopes);
