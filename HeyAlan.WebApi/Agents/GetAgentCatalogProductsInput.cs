namespace HeyAlan.WebApi.Agents;

using HeyAlan;

public sealed class GetAgentCatalogProductsInput
{
    public string? Query { get; init; }

    public int Skip { get; init; } = Constants.SkipDefault;

    public int Take { get; init; } = Constants.TakeDefault;
}
