namespace HeyAlan.WebApi.Agents;

public sealed record AgentCatalogProductAccessStateResult(
    Guid AgentId,
    Guid SubscriptionId,
    bool HasExplicitAssignments,
    IReadOnlyList<Guid> SubscriptionCatalogProductIds);
