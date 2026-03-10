namespace HeyAlan.WebApi.Agents;

using HeyAlan.Extensions;

public sealed record AgentCatalogProductItem(
    Guid SubscriptionCatalogProductId,
    string SquareItemId,
    string SquareVariationId,
    string ItemName,
    string VariationName,
    string? Description,
    string? Sku,
    long? BasePriceAmount,
    string? BasePriceCurrency,
    bool IsSellable,
    bool IsDeleted,
    bool IsAssigned);

public sealed record GetAgentCatalogProductsResult : CursorList<AgentCatalogProductItem>
{
    public bool HasExplicitAssignments { get; init; }

    public GetAgentCatalogProductsResult(
        IReadOnlyCollection<AgentCatalogProductItem> items,
        int skip,
        int take,
        bool hasExplicitAssignments)
        : base(items, skip, take)
    {
        this.HasExplicitAssignments = hasExplicitAssignments;
    }

    public GetAgentCatalogProductsResult()
    {
    }
}
