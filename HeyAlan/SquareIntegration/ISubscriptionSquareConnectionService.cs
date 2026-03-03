namespace HeyAlan.SquareIntegration;

public enum SquareConnectIntent
{
    Onboarding = 0,
    AdminSettings = 1
}

public sealed record StartSquareConnectInput(
    Guid SubscriptionId,
    Guid UserId,
    string? ReturnUrl,
    SquareConnectIntent Intent);

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

public sealed record ProbeSquareConnectionInput(
    Guid SubscriptionId,
    Guid UserId);

public abstract record ProbeSquareConnectionResult
{
    public sealed record Success(
        string MerchantId,
        DateTime AccessTokenExpiresAtUtc,
        string[] Scopes) : ProbeSquareConnectionResult;

    public sealed record Failure(string ErrorCode) : ProbeSquareConnectionResult;
}

public interface ISubscriptionSquareConnectionService
{
    Task<StartSquareConnectResult> StartConnectAsync(StartSquareConnectInput input, CancellationToken cancellationToken = default);

    Task<CompleteSquareConnectResult> CompleteConnectAsync(CompleteSquareConnectInput input, CancellationToken cancellationToken = default);

    Task<DisconnectSquareConnectionResult> DisconnectAsync(DisconnectSquareConnectionInput input, CancellationToken cancellationToken = default);

    Task<ProbeSquareConnectionResult> ProbeAsync(ProbeSquareConnectionInput input, CancellationToken cancellationToken = default);
}
