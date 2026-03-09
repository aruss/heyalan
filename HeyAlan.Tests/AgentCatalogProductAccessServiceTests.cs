namespace HeyAlan.Tests;

using HeyAlan.Agents;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using Microsoft.EntityFrameworkCore;

public class AgentCatalogProductAccessServiceTests
{
    [Fact]
    public async Task ReplaceAssignmentsAsync_WhenProductsBelongToAgentSubscription_ReplacesAssignmentsAtomically()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        SubscriptionCatalogProduct firstProduct = await SeedProductAsync(dbContext, subscriptionId, "item-1", "var-1");
        SubscriptionCatalogProduct secondProduct = await SeedProductAsync(dbContext, subscriptionId, "item-2", "var-2");
        SubscriptionCatalogProduct staleProduct = await SeedProductAsync(dbContext, subscriptionId, "item-3", "var-3");

        dbContext.AgentCatalogProductAccesses.Add(new AgentCatalogProductAccess
        {
            SubscriptionId = subscriptionId,
            AgentId = agentId,
            SubscriptionCatalogProductId = staleProduct.Id
        });

        await dbContext.SaveChangesAsync();

        AgentCatalogProductAccessService service = new(dbContext);
        ReplaceAgentCatalogProductAccessResult result = await service.ReplaceAssignmentsAsync(
            new ReplaceAgentCatalogProductAccessInput(agentId, [secondProduct.Id, firstProduct.Id]));

        ReplaceAgentCatalogProductAccessResult.Success success = Assert.IsType<ReplaceAgentCatalogProductAccessResult.Success>(result);
        Assert.True(success.State.HasExplicitAssignments);
        Assert.Equal([firstProduct.Id, secondProduct.Id], success.State.SubscriptionCatalogProductIds);

        List<Guid> persistedProductIds = await dbContext.AgentCatalogProductAccesses
            .Where(item => item.SubscriptionId == subscriptionId && item.AgentId == agentId)
            .OrderBy(item => item.SubscriptionCatalogProductId)
            .Select(item => item.SubscriptionCatalogProductId)
            .ToListAsync();

        Assert.Equal([firstProduct.Id, secondProduct.Id], persistedProductIds);
    }

    [Fact]
    public async Task ReplaceAssignmentsAsync_WhenProductBelongsToDifferentSubscription_ReturnsCatalogProductNotFound()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid otherSubscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        SubscriptionCatalogProduct crossSubscriptionProduct = await SeedProductAsync(
            dbContext,
            otherSubscriptionId,
            "item-1",
            "var-1");

        AgentCatalogProductAccessService service = new(dbContext);
        ReplaceAgentCatalogProductAccessResult result = await service.ReplaceAssignmentsAsync(
            new ReplaceAgentCatalogProductAccessInput(agentId, [crossSubscriptionProduct.Id]));

        ReplaceAgentCatalogProductAccessResult.Failure failure = Assert.IsType<ReplaceAgentCatalogProductAccessResult.Failure>(result);
        Assert.Equal("catalog_product_not_found", failure.ErrorCode);
        Assert.Empty(dbContext.AgentCatalogProductAccesses);
    }

    [Fact]
    public async Task ReplaceAssignmentsAsync_WhenInputContainsDuplicateOrEmptyIds_ReturnsAssignmentInvalid()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        SubscriptionCatalogProduct product = await SeedProductAsync(dbContext, subscriptionId, "item-1", "var-1");
        AgentCatalogProductAccessService service = new(dbContext);

        ReplaceAgentCatalogProductAccessResult duplicateResult = await service.ReplaceAssignmentsAsync(
            new ReplaceAgentCatalogProductAccessInput(agentId, [product.Id, product.Id]));

        ReplaceAgentCatalogProductAccessResult.Failure duplicateFailure = Assert.IsType<ReplaceAgentCatalogProductAccessResult.Failure>(duplicateResult);
        Assert.Equal("agent_catalog_assignment_invalid", duplicateFailure.ErrorCode);

        ReplaceAgentCatalogProductAccessResult emptyGuidResult = await service.ReplaceAssignmentsAsync(
            new ReplaceAgentCatalogProductAccessInput(agentId, [Guid.Empty]));

        ReplaceAgentCatalogProductAccessResult.Failure emptyGuidFailure = Assert.IsType<ReplaceAgentCatalogProductAccessResult.Failure>(emptyGuidResult);
        Assert.Equal("agent_catalog_assignment_invalid", emptyGuidFailure.ErrorCode);
    }

    [Fact]
    public async Task ClearAssignmentsAsync_WhenAssignmentsExist_RemovesAllAssignmentsAndReturnsDefaultAllMode()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        SubscriptionCatalogProduct product = await SeedProductAsync(dbContext, subscriptionId, "item-1", "var-1");
        dbContext.AgentCatalogProductAccesses.Add(new AgentCatalogProductAccess
        {
            SubscriptionId = subscriptionId,
            AgentId = agentId,
            SubscriptionCatalogProductId = product.Id
        });

        await dbContext.SaveChangesAsync();

        AgentCatalogProductAccessService service = new(dbContext);
        ClearAgentCatalogProductAccessResult result = await service.ClearAssignmentsAsync(
            new ClearAgentCatalogProductAccessInput(agentId));

        ClearAgentCatalogProductAccessResult.Success success = Assert.IsType<ClearAgentCatalogProductAccessResult.Success>(result);
        Assert.False(success.State.HasExplicitAssignments);
        Assert.Empty(success.State.SubscriptionCatalogProductIds);
        Assert.Empty(dbContext.AgentCatalogProductAccesses);
    }

    [Fact]
    public async Task GetStateAsync_WhenAssignmentsExist_ReturnsOrderedAssignmentState()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        SubscriptionCatalogProduct secondProduct = await SeedProductAsync(dbContext, subscriptionId, "item-2", "var-2");
        SubscriptionCatalogProduct firstProduct = await SeedProductAsync(dbContext, subscriptionId, "item-1", "var-1");

        dbContext.AgentCatalogProductAccesses.AddRange(
            new AgentCatalogProductAccess
            {
                SubscriptionId = subscriptionId,
                AgentId = agentId,
                SubscriptionCatalogProductId = secondProduct.Id
            },
            new AgentCatalogProductAccess
            {
                SubscriptionId = subscriptionId,
                AgentId = agentId,
                SubscriptionCatalogProductId = firstProduct.Id
            });

        await dbContext.SaveChangesAsync();

        AgentCatalogProductAccessService service = new(dbContext);
        GetAgentCatalogProductAccessStateOperationResult result = await service.GetStateAsync(
            new GetAgentCatalogProductAccessStateInput(agentId));

        GetAgentCatalogProductAccessStateOperationResult.Success success = Assert.IsType<GetAgentCatalogProductAccessStateOperationResult.Success>(result);
        Assert.True(success.State.HasExplicitAssignments);
        Assert.Equal([firstProduct.Id, secondProduct.Id], success.State.SubscriptionCatalogProductIds);
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }

    private static async Task SeedAgentAsync(MainDataContext dbContext, Guid subscriptionId, Guid agentId)
    {
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId
        });

        dbContext.Agents.Add(new Agent
        {
            Id = agentId,
            SubscriptionId = subscriptionId,
            Name = "Agent"
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task<SubscriptionCatalogProduct> SeedProductAsync(
        MainDataContext dbContext,
        Guid subscriptionId,
        string squareItemId,
        string squareVariationId)
    {
        bool subscriptionExists = await dbContext.Subscriptions
            .AnyAsync(item => item.Id == subscriptionId);

        if (!subscriptionExists)
        {
            dbContext.Subscriptions.Add(new Subscription
            {
                Id = subscriptionId
            });
        }

        SubscriptionCatalogProduct product = new()
        {
            SubscriptionId = subscriptionId,
            SquareItemId = squareItemId,
            SquareVariationId = squareVariationId,
            ItemName = squareItemId,
            VariationName = squareVariationId,
            IsSellable = true,
            IsDeleted = false,
            SearchText = $"{squareItemId} {squareVariationId}"
        };

        dbContext.SubscriptionCatalogProducts.Add(product);
        await dbContext.SaveChangesAsync();
        return product;
    }
}
