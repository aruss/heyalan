namespace HeyAlan.SquareIntegration;

public sealed record SquareMerchantProfile(
    string MerchantId);

public abstract record SquareMerchantProfileResult
{
    public sealed record Success(SquareMerchantProfile Profile) : SquareMerchantProfileResult;

    public sealed record ReconnectRequired : SquareMerchantProfileResult;

    public sealed record Failure(string ErrorCode) : SquareMerchantProfileResult;
}

public interface ISquareMerchantClient
{
    Task<SquareMerchantProfileResult> GetMerchantProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}
