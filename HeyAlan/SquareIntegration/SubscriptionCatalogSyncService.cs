namespace HeyAlan.SquareIntegration;

using System.Collections.Concurrent;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Square;
using Square.Catalog;

public sealed class SubscriptionCatalogSyncService : ISubscriptionCatalogSyncService
{
    private const int SearchPageLimit = 1000;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> SyncLocks = new();

    private readonly MainDataContext dbContext;
    private readonly ISquareService squareService;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly AppOptions appOptions;
    private readonly ILogger<SubscriptionCatalogSyncService> logger;

    public SubscriptionCatalogSyncService(
        MainDataContext dbContext,
        ISquareService squareService,
        IHttpClientFactory httpClientFactory,
        AppOptions appOptions,
        ILogger<SubscriptionCatalogSyncService> logger)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.squareService = squareService ?? throw new ArgumentNullException(nameof(squareService));
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.appOptions = appOptions ?? throw new ArgumentNullException(nameof(appOptions));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SubscriptionCatalogSyncResult> SyncAsync(
        SubscriptionCatalogSyncInput input,
        CancellationToken cancellationToken = default)
    {
        SemaphoreSlim syncLock = SyncLocks.GetOrAdd(input.SubscriptionId, static _ => new SemaphoreSlim(1, 1));
        await syncLock.WaitAsync(cancellationToken);

        try
        {
            SubscriptionCatalogSyncState syncState = await this.GetOrCreateSyncStateAsync(input.SubscriptionId, cancellationToken);
            DateTime syncStartedAtUtc = DateTime.UtcNow;
            bool isFullSync = input.ForceFullSync || syncState.LastSyncedBeginTimeUtc is null;

            syncState.SyncInProgress = true;
            syncState.LastSyncStartedAtUtc = syncStartedAtUtc;
            syncState.LastTriggerSource = input.TriggerSource;
            syncState.LastErrorCode = null;
            syncState.LastErrorMessage = null;
            await this.dbContext.SaveChangesAsync(cancellationToken);

            SquareTokenResolution tokenResolution = await this.squareService.GetValidAccessTokenAsync(
                input.SubscriptionId,
                cancellationToken);

            if (tokenResolution is not SquareTokenResolution.Success tokenSuccess)
            {
                return await this.FailAsync(
                    syncState,
                    syncStartedAtUtc,
                    MapTokenFailureCode(tokenResolution),
                    "Square access token was not available for catalog sync.",
                    cancellationToken);
            }

            try
            {
                IReadOnlyCollection<CatalogObject> catalogObjects = await this.FetchCatalogObjectsAsync(
                    tokenSuccess.AccessToken,
                    syncState.LastSyncedBeginTimeUtc,
                    isFullSync,
                    cancellationToken);

                CatalogSyncProjection projection = BuildProjection(catalogObjects);

                await this.ApplyProjectionAsync(
                    input.SubscriptionId,
                    projection,
                    isFullSync,
                    cancellationToken);

                DateTime syncCompletedAtUtc = DateTime.UtcNow;
                syncState.LastSyncedBeginTimeUtc = syncStartedAtUtc;
                syncState.LastSyncCompletedAtUtc = syncCompletedAtUtc;
                syncState.SyncInProgress = false;
                syncState.LastErrorCode = null;
                syncState.LastErrorMessage = null;
                await this.dbContext.SaveChangesAsync(cancellationToken);

                this.logger.LogInformation(
                    "Completed Square catalog sync for subscription {SubscriptionId} with {ProductCount} products.",
                    input.SubscriptionId,
                    projection.Variations.Count);

                return new SubscriptionCatalogSyncResult.Success(
                    projection.Variations.Count,
                    projection.Variations.Values.Sum(item => item.Locations.Count),
                    syncStartedAtUtc,
                    syncCompletedAtUtc,
                    isFullSync);
            }
            catch (SquareApiException exception)
            {
                this.logger.LogWarning(
                    exception,
                    "Square catalog sync failed for subscription {SubscriptionId}.",
                    input.SubscriptionId);

                return await this.FailAsync(
                    syncState,
                    syncStartedAtUtc,
                    "square_catalog_search_failed",
                    "Square catalog search failed.",
                    cancellationToken);
            }
            catch (Exception exception)
            {
                this.logger.LogWarning(
                    exception,
                    "Catalog sync failed for subscription {SubscriptionId}.",
                    input.SubscriptionId);

                return await this.FailAsync(
                    syncState,
                    syncStartedAtUtc,
                    "subscription_catalog_sync_failed",
                    "Subscription catalog sync failed.",
                    cancellationToken);
            }
        }
        finally
        {
            syncLock.Release();
        }
    }

    private async Task<IReadOnlyCollection<CatalogObject>> FetchCatalogObjectsAsync(
        string accessToken,
        DateTime? beginTimeUtc,
        bool isFullSync,
        CancellationToken cancellationToken)
    {
        SquareClient client = this.CreateAccessTokenClient(accessToken);
        Dictionary<string, CatalogObject> objects = new(StringComparer.Ordinal);
        string? cursor = null;

        do
        {
            SearchCatalogObjectsRequest request = new()
            {
                Cursor = cursor,
                IncludeDeletedObjects = !isFullSync,
                IncludeRelatedObjects = true,
                Limit = SearchPageLimit,
                ObjectTypes =
                [
                    CatalogObjectType.Item,
                    CatalogObjectType.ItemVariation
                ]
            };

            if (!isFullSync && beginTimeUtc is not null)
            {
                request.BeginTime = beginTimeUtc.Value.ToString("O");
            }

            SearchCatalogObjectsResponse response = await client.Catalog.SearchAsync(request, null, cancellationToken);
            UpsertCatalogObjects(objects, response.Objects);
            UpsertCatalogObjects(objects, response.RelatedObjects);
            cursor = response.Cursor;
        }
        while (!String.IsNullOrWhiteSpace(cursor));

        return objects.Values.ToList();
    }

    private async Task ApplyProjectionAsync(
        Guid subscriptionId,
        CatalogSyncProjection projection,
        bool isFullSync,
        CancellationToken cancellationToken)
    {
        Dictionary<string, SubscriptionCatalogProduct> productsByVariationId = await this.dbContext.SubscriptionCatalogProducts
            .Where(item => item.SubscriptionId == subscriptionId)
            .ToDictionaryAsync(item => item.SquareVariationId, StringComparer.Ordinal, cancellationToken);

        foreach (KeyValuePair<string, CatalogItemChange> itemEntry in projection.Items)
        {
            CatalogItemChange itemChange = itemEntry.Value;
            List<SubscriptionCatalogProduct> linkedProducts = productsByVariationId.Values
                .Where(product => product.SquareItemId == itemChange.SquareItemId)
                .ToList();

            foreach (SubscriptionCatalogProduct product in linkedProducts)
            {
                product.ItemName = itemChange.ItemName;
                product.Description = itemChange.Description;
                if (itemChange.IsDeleted)
                {
                    product.IsDeleted = true;
                }
            }
        }

        foreach (KeyValuePair<string, CatalogVariationChange> variationEntry in projection.Variations)
        {
            CatalogVariationChange variationChange = variationEntry.Value;
            CatalogItemChange? itemChange = projection.Items.GetValueOrDefault(variationChange.SquareItemId);

            SubscriptionCatalogProduct? existingProduct = productsByVariationId.GetValueOrDefault(variationChange.SquareVariationId);
            if (existingProduct is null)
            {
                existingProduct = new SubscriptionCatalogProduct
                {
                    SubscriptionId = subscriptionId,
                    SquareItemId = variationChange.SquareItemId,
                    SquareVariationId = variationChange.SquareVariationId,
                    ItemName = ResolveItemName(itemChange, variationChange),
                    Description = ResolveDescription(itemChange),
                    SearchText = String.Empty
                };

                this.dbContext.SubscriptionCatalogProducts.Add(existingProduct);
                productsByVariationId[variationChange.SquareVariationId] = existingProduct;
            }
            else
            {
                existingProduct.SquareItemId = variationChange.SquareItemId;
                existingProduct.ItemName = ResolveItemName(itemChange, variationChange, existingProduct.ItemName);
                existingProduct.Description = ResolveDescription(itemChange, existingProduct.Description);
            }

            existingProduct.VariationName = variationChange.VariationName;
            existingProduct.Sku = variationChange.Sku;
            existingProduct.BasePriceAmount = variationChange.BasePriceAmount;
            existingProduct.BasePriceCurrency = variationChange.BasePriceCurrency;
            existingProduct.IsSellable = variationChange.IsSellable;
            existingProduct.IsDeleted = variationChange.IsDeleted || itemChange?.IsDeleted == true;
            existingProduct.SquareUpdatedAtUtc = variationChange.SquareUpdatedAtUtc;
            existingProduct.SquareVersion = variationChange.SquareVersion;
            existingProduct.SearchText = BuildSearchText(
                existingProduct.ItemName,
                existingProduct.VariationName,
                existingProduct.Description,
                existingProduct.Sku);
        }

        if (isFullSync)
        {
            HashSet<string> presentVariationIds = projection.Variations.Keys.ToHashSet(StringComparer.Ordinal);
            foreach (SubscriptionCatalogProduct product in productsByVariationId.Values)
            {
                if (!presentVariationIds.Contains(product.SquareVariationId))
                {
                    product.IsDeleted = true;
                }
            }
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);

        Dictionary<string, SubscriptionCatalogProductLocation> locationLookup = await this.dbContext.SubscriptionCatalogProductLocations
            .Where(item => item.SubscriptionId == subscriptionId)
            .ToDictionaryAsync(
                item => BuildLocationKey(item.SquareVariationId, item.LocationId),
                StringComparer.Ordinal,
                cancellationToken);

        HashSet<string> syncedLocationKeys = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, CatalogVariationChange> variationEntry in projection.Variations)
        {
            SubscriptionCatalogProduct product = productsByVariationId[variationEntry.Key];
            foreach (CatalogLocationOverrideChange locationChange in variationEntry.Value.Locations)
            {
                string locationKey = BuildLocationKey(locationChange.SquareVariationId, locationChange.LocationId);
                syncedLocationKeys.Add(locationKey);

                SubscriptionCatalogProductLocation? existingLocation = locationLookup.GetValueOrDefault(locationKey);
                if (existingLocation is null)
                {
                    existingLocation = new SubscriptionCatalogProductLocation
                    {
                        SubscriptionId = subscriptionId,
                        SubscriptionCatalogProductId = product.Id,
                        SquareVariationId = locationChange.SquareVariationId,
                        LocationId = locationChange.LocationId
                    };

                    this.dbContext.SubscriptionCatalogProductLocations.Add(existingLocation);
                    locationLookup[locationKey] = existingLocation;
                }
                else
                {
                    existingLocation.SubscriptionCatalogProductId = product.Id;
                }

                existingLocation.PriceOverrideAmount = locationChange.PriceOverrideAmount;
                existingLocation.PriceOverrideCurrency = locationChange.PriceOverrideCurrency;
                existingLocation.IsAvailableForSale = locationChange.IsAvailableForSale;
                existingLocation.IsSoldOut = locationChange.IsSoldOut;
            }
        }

        List<SubscriptionCatalogProductLocation> locationsToRemove = locationLookup.Values
            .Where(
                item =>
                    projection.Variations.ContainsKey(item.SquareVariationId) &&
                    !syncedLocationKeys.Contains(BuildLocationKey(item.SquareVariationId, item.LocationId)))
            .ToList();

        if (locationsToRemove.Count > 0)
        {
            this.dbContext.SubscriptionCatalogProductLocations.RemoveRange(locationsToRemove);
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SubscriptionCatalogSyncState> GetOrCreateSyncStateAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        SubscriptionCatalogSyncState? existingState = await this.dbContext.SubscriptionCatalogSyncStates
            .SingleOrDefaultAsync(item => item.SubscriptionId == subscriptionId, cancellationToken);

        if (existingState is not null)
        {
            return existingState;
        }

        SubscriptionCatalogSyncState createdState = new()
        {
            SubscriptionId = subscriptionId,
            SyncInProgress = false,
            PendingResync = false
        };

        this.dbContext.SubscriptionCatalogSyncStates.Add(createdState);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        return createdState;
    }

    private async Task<SubscriptionCatalogSyncResult.Failure> FailAsync(
        SubscriptionCatalogSyncState syncState,
        DateTime syncStartedAtUtc,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        syncState.SyncInProgress = false;
        syncState.LastSyncStartedAtUtc = syncStartedAtUtc;
        syncState.LastSyncCompletedAtUtc = DateTime.UtcNow;
        syncState.LastErrorCode = errorCode;
        syncState.LastErrorMessage = errorMessage;
        await this.dbContext.SaveChangesAsync(cancellationToken);

        return new SubscriptionCatalogSyncResult.Failure(errorCode);
    }

    private SquareClient CreateAccessTokenClient(string accessToken)
    {
        string baseUrl = SquareIntegrationRules.ResolveOAuthBaseUrl(this.appOptions.SquareClientId!);
        ClientOptions options = new()
        {
            BaseUrl = baseUrl,
            HttpClient = this.httpClientFactory.CreateClient("SquareOAuthClient")
        };

        return new SquareClient(accessToken, clientOptions: options);
    }

    private static CatalogSyncProjection BuildProjection(IReadOnlyCollection<CatalogObject> catalogObjects)
    {
        CatalogSyncProjection projection = new();

        foreach (CatalogObject catalogObject in catalogObjects)
        {
            if (catalogObject.TryAsItem(out CatalogObjectItem? itemObject))
            {
                CatalogItem? itemData = itemObject.ItemData;
                if (itemData is not null)
                {
                    projection.Items[itemObject.Id] = new CatalogItemChange(
                        itemObject.Id,
                        itemData.Name?.Trim() ?? itemObject.Id,
                        NormalizeOptional(itemData.Description),
                        itemObject.IsDeleted == true);
                }

                continue;
            }

            if (!catalogObject.TryAsItemVariation(out CatalogObjectItemVariation? variationObject))
            {
                continue;
            }

            CatalogItemVariation? variationData = variationObject.ItemVariationData;
            if (variationData is null || String.IsNullOrWhiteSpace(variationObject.Id))
            {
                continue;
            }

            string squareItemId = variationData.ItemId?.Trim() ?? String.Empty;
            if (String.IsNullOrWhiteSpace(squareItemId))
            {
                continue;
            }

            projection.Variations[variationObject.Id] = new CatalogVariationChange(
                squareItemId,
                variationObject.Id,
                NormalizeVariationName(variationData.Name, variationObject.Id),
                NormalizeOptional(variationData.Sku),
                variationData.PriceMoney?.Amount,
                variationData.PriceMoney?.Currency?.ToString(),
                variationData.Sellable != false,
                variationObject.IsDeleted == true,
                ParseSquareTimestamp(variationObject.UpdatedAt),
                variationObject.Version,
                BuildLocationOverrides(variationObject, variationData));
        }

        return projection;
    }

    private static List<CatalogLocationOverrideChange> BuildLocationOverrides(
        CatalogObjectItemVariation variationObject,
        CatalogItemVariation variationData)
    {
        List<CatalogLocationOverrideChange> locations = [];
        if (variationData.LocationOverrides is null)
        {
            return locations;
        }

        foreach (ItemVariationLocationOverrides overrideData in variationData.LocationOverrides)
        {
            string? locationId = NormalizeOptional(overrideData.LocationId);
            if (String.IsNullOrWhiteSpace(locationId))
            {
                continue;
            }

            locations.Add(
                new CatalogLocationOverrideChange(
                    variationObject.Id,
                    locationId,
                    overrideData.PriceMoney?.Amount,
                    overrideData.PriceMoney?.Currency?.ToString(),
                    ResolveIsAvailableForSale(variationObject, locationId),
                    overrideData.SoldOut == true));
        }

        return locations;
    }

    private static bool ResolveIsAvailableForSale(CatalogObjectItemVariation variationObject, string locationId)
    {
        IReadOnlyCollection<string> absentAtLocationIds = variationObject.AbsentAtLocationIds?.ToArray() ?? [];
        IReadOnlyCollection<string> presentAtLocationIds = variationObject.PresentAtLocationIds?.ToArray() ?? [];
        bool presentAtAllLocations = variationObject.PresentAtAllLocations != false;

        if (presentAtAllLocations)
        {
            return !absentAtLocationIds.Contains(locationId, StringComparer.Ordinal);
        }

        return presentAtLocationIds.Contains(locationId, StringComparer.Ordinal);
    }

    private static void UpsertCatalogObjects(Dictionary<string, CatalogObject> objects, IEnumerable<CatalogObject>? catalogObjects)
    {
        if (catalogObjects is null)
        {
            return;
        }

        foreach (CatalogObject catalogObject in catalogObjects)
        {
            string? key = GetCatalogObjectKey(catalogObject);
            if (String.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            objects[key] = catalogObject;
        }
    }

    private static string? GetCatalogObjectKey(CatalogObject catalogObject)
    {
        if (catalogObject.TryAsItem(out CatalogObjectItem? itemObject))
        {
            return $"{CatalogObjectType.Item}:{itemObject.Id}";
        }

        if (catalogObject.TryAsItemVariation(out CatalogObjectItemVariation? variationObject))
        {
            return $"{CatalogObjectType.ItemVariation}:{variationObject.Id}";
        }

        return null;
    }

    private static string BuildSearchText(
        string itemName,
        string variationName,
        string? description,
        string? sku)
    {
        List<string> parts =
        [
            itemName,
            variationName
        ];

        if (!String.IsNullOrWhiteSpace(description))
        {
            parts.Add(description);
        }

        if (!String.IsNullOrWhiteSpace(sku))
        {
            parts.Add(sku);
        }

        return String.Join(' ', parts)
            .Trim()
            .ToLowerInvariant();
    }

    private static string ResolveItemName(
        CatalogItemChange? itemChange,
        CatalogVariationChange variationChange,
        string? fallbackValue = null)
    {
        if (itemChange is not null && !String.IsNullOrWhiteSpace(itemChange.ItemName))
        {
            return itemChange.ItemName;
        }

        if (!String.IsNullOrWhiteSpace(fallbackValue))
        {
            return fallbackValue;
        }

        return variationChange.VariationName;
    }

    private static string? ResolveDescription(CatalogItemChange? itemChange, string? fallbackValue = null)
    {
        if (itemChange is not null)
        {
            return itemChange.Description;
        }

        return fallbackValue;
    }

    private static string NormalizeVariationName(string? variationName, string squareVariationId)
    {
        if (!String.IsNullOrWhiteSpace(variationName))
        {
            return variationName.Trim();
        }

        return squareVariationId;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static DateTime? ParseSquareTimestamp(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTime.TryParse(
                value,
                null,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out DateTime parsedValue))
        {
            return null;
        }

        return parsedValue;
    }

    private static string BuildLocationKey(string squareVariationId, string locationId)
    {
        return $"{squareVariationId}::{locationId}";
    }

    private static string MapTokenFailureCode(SquareTokenResolution tokenResolution)
    {
        return tokenResolution switch
        {
            SquareTokenResolution.ConnectionMissing => "square_connection_missing",
            SquareTokenResolution.ReconnectRequired reconnectRequired => reconnectRequired.ReasonCode,
            SquareTokenResolution.RefreshFailed refreshFailed => refreshFailed.ReasonCode,
            _ => "square_access_token_unavailable"
        };
    }

    private sealed class CatalogSyncProjection
    {
        public Dictionary<string, CatalogItemChange> Items { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, CatalogVariationChange> Variations { get; } = new(StringComparer.Ordinal);
    }

    private sealed record CatalogItemChange(
        string SquareItemId,
        string ItemName,
        string? Description,
        bool IsDeleted);

    private sealed record CatalogVariationChange(
        string SquareItemId,
        string SquareVariationId,
        string VariationName,
        string? Sku,
        long? BasePriceAmount,
        string? BasePriceCurrency,
        bool IsSellable,
        bool IsDeleted,
        DateTime? SquareUpdatedAtUtc,
        long? SquareVersion,
        IReadOnlyList<CatalogLocationOverrideChange> Locations);

    private sealed record CatalogLocationOverrideChange(
        string SquareVariationId,
        string LocationId,
        long? PriceOverrideAmount,
        string? PriceOverrideCurrency,
        bool IsAvailableForSale,
        bool IsSoldOut);
}
