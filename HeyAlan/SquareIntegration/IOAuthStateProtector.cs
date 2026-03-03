namespace HeyAlan.SquareIntegration;

public sealed record SquareConnectStatePayload(
    Guid SubscriptionId,
    Guid UserId,
    string ReturnUrl,
    SquareConnectIntent Intent,
    DateTime IssuedAtUtc);

public interface IOAuthStateProtector
{
    string Protect(SquareConnectStatePayload payload);

    bool TryUnprotect(string protectedState, out SquareConnectStatePayload? payload);
}
