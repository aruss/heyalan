namespace HeyAlan.WebApi.Agents;

public sealed record AgentSalesZipCodeStateResult(
    Guid AgentId,
    Guid SubscriptionId,
    bool HasExplicitZipRestrictions,
    IReadOnlyList<string> ZipCodesNormalized);
