namespace HeyAlan.Agents;

public sealed record GetAgentSalesZipCodeStateInput(
    Guid AgentId);

public sealed record AgentSalesZipCodeStateResult(
    Guid AgentId,
    Guid SubscriptionId,
    bool HasExplicitZipRestrictions,
    IReadOnlyList<string> ZipCodesNormalized);

public abstract record GetAgentSalesZipCodeStateOperationResult
{
    public sealed record Success(AgentSalesZipCodeStateResult State) : GetAgentSalesZipCodeStateOperationResult;

    public sealed record Failure(string ErrorCode) : GetAgentSalesZipCodeStateOperationResult;
}

public sealed record ReplaceAgentSalesZipCodesInput(
    Guid AgentId,
    IReadOnlyCollection<string> ZipCodes);

public abstract record ReplaceAgentSalesZipCodesResult
{
    public sealed record Success(AgentSalesZipCodeStateResult State) : ReplaceAgentSalesZipCodesResult;

    public sealed record Failure(string ErrorCode) : ReplaceAgentSalesZipCodesResult;
}

public sealed record ClearAgentSalesZipCodesInput(
    Guid AgentId);

public abstract record ClearAgentSalesZipCodesResult
{
    public sealed record Success(AgentSalesZipCodeStateResult State) : ClearAgentSalesZipCodesResult;

    public sealed record Failure(string ErrorCode) : ClearAgentSalesZipCodesResult;
}

public interface IAgentSalesZipCodeService
{
    Task<GetAgentSalesZipCodeStateOperationResult> GetStateAsync(
        GetAgentSalesZipCodeStateInput input,
        CancellationToken cancellationToken = default);

    Task<ReplaceAgentSalesZipCodesResult> ReplaceZipCodesAsync(
        ReplaceAgentSalesZipCodesInput input,
        CancellationToken cancellationToken = default);

    Task<ClearAgentSalesZipCodesResult> ClearZipCodesAsync(
        ClearAgentSalesZipCodesInput input,
        CancellationToken cancellationToken = default);
}
