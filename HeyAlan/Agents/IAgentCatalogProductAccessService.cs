namespace HeyAlan.Agents;

public sealed record GetAgentCatalogProductAccessStateInput(
    Guid AgentId);

public sealed record AgentCatalogProductAccessStateResult(
    Guid AgentId,
    Guid SubscriptionId,
    bool HasExplicitAssignments,
    IReadOnlyList<Guid> SubscriptionCatalogProductIds);

public abstract record GetAgentCatalogProductAccessStateOperationResult
{
    public sealed record Success(AgentCatalogProductAccessStateResult State) : GetAgentCatalogProductAccessStateOperationResult;

    public sealed record Failure(string ErrorCode) : GetAgentCatalogProductAccessStateOperationResult;
}

public sealed record ReplaceAgentCatalogProductAccessInput(
    Guid AgentId,
    IReadOnlyCollection<Guid> SubscriptionCatalogProductIds);

public abstract record ReplaceAgentCatalogProductAccessResult
{
    public sealed record Success(AgentCatalogProductAccessStateResult State) : ReplaceAgentCatalogProductAccessResult;

    public sealed record Failure(string ErrorCode) : ReplaceAgentCatalogProductAccessResult;
}

public sealed record ClearAgentCatalogProductAccessInput(
    Guid AgentId);

public abstract record ClearAgentCatalogProductAccessResult
{
    public sealed record Success(AgentCatalogProductAccessStateResult State) : ClearAgentCatalogProductAccessResult;

    public sealed record Failure(string ErrorCode) : ClearAgentCatalogProductAccessResult;
}

public interface IAgentCatalogProductAccessService
{
    Task<GetAgentCatalogProductAccessStateOperationResult> GetStateAsync(
        GetAgentCatalogProductAccessStateInput input,
        CancellationToken cancellationToken = default);

    Task<ReplaceAgentCatalogProductAccessResult> ReplaceAssignmentsAsync(
        ReplaceAgentCatalogProductAccessInput input,
        CancellationToken cancellationToken = default);

    Task<ClearAgentCatalogProductAccessResult> ClearAssignmentsAsync(
        ClearAgentCatalogProductAccessInput input,
        CancellationToken cancellationToken = default);
}
