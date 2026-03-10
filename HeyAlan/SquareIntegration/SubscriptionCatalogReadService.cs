namespace HeyAlan.SquareIntegration;

using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.Extensions;
using Microsoft.EntityFrameworkCore;

public sealed class SubscriptionCatalogReadService : ISubscriptionCatalogReadService
{
    private readonly MainDataContext dbContext;

    public SubscriptionCatalogReadService(MainDataContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<GetSubscriptionCatalogProductsResult> GetProductsAsync(
        GetSubscriptionCatalogProductsInput input,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SubscriptionCatalogProduct> baseQuery = await this.BuildAgentFilteredQueryAsync(
            input.SubscriptionId,
            input.AgentId,
            cancellationToken);

        string? normalizedQuery = NormalizeQuery(input.Query);
        if (!String.IsNullOrWhiteSpace(normalizedQuery))
        {
            baseQuery = baseQuery.Where(product => product.SearchText.Contains(normalizedQuery));
        }

        int skip = Math.Clamp(input.Skip, Constants.SkipMin, Constants.SkipMax);
        int take = Math.Clamp(input.Take, Constants.TakeMin, Constants.TakeMax);

        PagedList<SubscriptionCatalogProduct> products = await baseQuery
            .Include(product => product.Locations)
            .OrderBy(product => product.ItemName)
            .ThenBy(product => product.VariationName)
            .ThenBy(product => product.Id)
            .ToPagedListAsync(skip, take, cancellationToken);

        List<SubscriptionCatalogProductResult> items = products.Items
            .Select(MapProduct)
            .ToList();

        SubscriptionCatalogFreshnessResult freshness = await this.GetFreshnessAsync(input.SubscriptionId, cancellationToken);
        return new GetSubscriptionCatalogProductsResult(
            new PagedList<SubscriptionCatalogProductResult>(items, products.Total, products.Skip, products.Take, products.Sort),
            freshness);
    }

    public async Task<SubscriptionCatalogProductResult?> GetProductByCatalogProductIdAsync(
        Guid subscriptionId,
        Guid agentId,
        Guid subscriptionCatalogProductId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SubscriptionCatalogProduct> baseQuery = await this.BuildAgentFilteredQueryAsync(
            subscriptionId,
            agentId,
            cancellationToken);

        SubscriptionCatalogProduct? product = await baseQuery
            .Include(item => item.Locations)
            .SingleOrDefaultAsync(item => item.Id == subscriptionCatalogProductId, cancellationToken);

        return product is null ? null : MapProduct(product);
    }

    public async Task<SubscriptionCatalogProductResult?> GetProductBySquareVariationIdAsync(
        Guid subscriptionId,
        Guid agentId,
        string squareVariationId,
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(squareVariationId))
        {
            return null;
        }

        string normalizedVariationId = squareVariationId.Trim();
        IQueryable<SubscriptionCatalogProduct> baseQuery = await this.BuildAgentFilteredQueryAsync(
            subscriptionId,
            agentId,
            cancellationToken);

        SubscriptionCatalogProduct? product = await baseQuery
            .Include(item => item.Locations)
            .SingleOrDefaultAsync(item => item.SquareVariationId == normalizedVariationId, cancellationToken);

        return product is null ? null : MapProduct(product);
    }

    public async Task<SubscriptionCatalogFreshnessResult> GetFreshnessAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        SubscriptionCatalogSyncState? syncState = await this.dbContext.SubscriptionCatalogSyncStates
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.SubscriptionId == subscriptionId, cancellationToken);

        if (syncState is null)
        {
            return new SubscriptionCatalogFreshnessResult(
                null,
                null,
                null,
                null,
                null,
                false,
                false,
                null,
                null);
        }

        return new SubscriptionCatalogFreshnessResult(
            syncState.LastSyncedBeginTimeUtc,
            syncState.NextScheduledSyncAtUtc,
            syncState.LastSyncStartedAtUtc,
            syncState.LastSyncCompletedAtUtc,
            syncState.LastTriggerSource,
            syncState.SyncInProgress,
            syncState.PendingResync,
            syncState.LastErrorCode,
            syncState.LastErrorMessage);
    }

    private async Task<IQueryable<SubscriptionCatalogProduct>> BuildAgentFilteredQueryAsync(
        Guid subscriptionId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        IQueryable<SubscriptionCatalogProduct> query = this.dbContext.SubscriptionCatalogProducts
            .AsNoTracking()
            .Where(product =>
                product.SubscriptionId == subscriptionId &&
                product.IsSellable &&
                !product.IsDeleted);

        bool hasAssignments = await this.dbContext.AgentCatalogProductAccesses
            .AsNoTracking()
            .AnyAsync(
                item =>
                    item.SubscriptionId == subscriptionId &&
                    item.AgentId == agentId,
                cancellationToken);

        if (!hasAssignments)
        {
            return query;
        }

        return query.Where(
            product =>
                this.dbContext.AgentCatalogProductAccesses.Any(
                    access =>
                        access.SubscriptionId == subscriptionId &&
                        access.AgentId == agentId &&
                        access.SubscriptionCatalogProductId == product.Id));
    }

    private static SubscriptionCatalogProductResult MapProduct(SubscriptionCatalogProduct product)
    {
        List<SubscriptionCatalogProductLocationResult> locations = product.Locations
            .OrderBy(location => location.LocationId)
            .Select(
                location =>
                    new SubscriptionCatalogProductLocationResult(
                        location.LocationId,
                        location.PriceOverrideAmount,
                        location.PriceOverrideCurrency,
                        location.IsAvailableForSale,
                        location.IsSoldOut))
            .ToList();

        return new SubscriptionCatalogProductResult(
            product.Id,
            product.SquareItemId,
            product.SquareVariationId,
            product.ItemName,
            product.VariationName,
            product.Description,
            product.Sku,
            product.BasePriceAmount,
            product.BasePriceCurrency,
            product.IsSellable,
            product.IsDeleted,
            product.SquareUpdatedAtUtc,
            product.SquareVersion,
            locations);
    }

    private static string? NormalizeQuery(string? query)
    {
        if (String.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        return query.Trim().ToLowerInvariant();
    }
}
