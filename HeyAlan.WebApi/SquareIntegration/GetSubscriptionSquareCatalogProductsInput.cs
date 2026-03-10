namespace HeyAlan.WebApi.SquareIntegration;

using System.ComponentModel.DataAnnotations;
using HeyAlan;

public sealed class GetSubscriptionSquareCatalogProductsInput
{
    public string? Query { get; init; }

    [Range(Constants.SkipMin, Constants.SkipMax)]
    public int Skip { get; init; } = Constants.SkipDefault;

    [Range(Constants.TakeMin, Constants.TakeMax)]
    public int Take { get; init; } = Constants.TakeDefault;
}
