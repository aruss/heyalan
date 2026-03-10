namespace HeyAlan.Tests;

using HeyAlan.Agents;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using Microsoft.EntityFrameworkCore;

public class AgentSalesZipCodeServiceTests
{
    [Fact]
    public void TryNormalizeZipCode_WhenValueIsFiveDigitZip_ReturnsNormalizedZip()
    {
        bool isValid = AgentSalesZipCodeRules.TryNormalizeZipCode(" 12345 ", out string normalizedZipCode);

        Assert.True(isValid);
        Assert.Equal("12345", normalizedZipCode);
    }

    [Fact]
    public void TryNormalizeZipCode_WhenValueIsZipPlusFourWithHyphen_ReturnsDigitsOnlyNormalizedZip()
    {
        bool isValid = AgentSalesZipCodeRules.TryNormalizeZipCode("12345-6789", out string normalizedZipCode);

        Assert.True(isValid);
        Assert.Equal("123456789", normalizedZipCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("ABCDE")]
    [InlineData("1234A")]
    [InlineData("12345-678")]
    public void TryNormalizeZipCode_WhenValueIsInvalid_ReturnsFalse(string rawZipCode)
    {
        bool isValid = AgentSalesZipCodeRules.TryNormalizeZipCode(rawZipCode, out string normalizedZipCode);

        Assert.False(isValid);
        Assert.Equal(String.Empty, normalizedZipCode);
    }

    [Fact]
    public async Task ReplaceZipCodesAsync_WhenZipCodesAreValid_ReplacesZipAllowlistAtomically()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        dbContext.AgentSalesZipCodes.Add(new AgentSalesZipCode
        {
            SubscriptionId = subscriptionId,
            AgentId = agentId,
            ZipCodeNormalized = "99999"
        });

        await dbContext.SaveChangesAsync();

        AgentSalesZipCodeService service = new(dbContext);
        ReplaceAgentSalesZipCodesResult result = await service.ReplaceZipCodesAsync(
            new ReplaceAgentSalesZipCodesInput(agentId, ["12345-6789", "54321"]));

        ReplaceAgentSalesZipCodesResult.Success success = Assert.IsType<ReplaceAgentSalesZipCodesResult.Success>(result);
        Assert.True(success.State.HasExplicitZipRestrictions);
        Assert.Equal(["123456789", "54321"], success.State.ZipCodesNormalized);

        List<string> persistedZipCodes = await dbContext.AgentSalesZipCodes
            .Where(item => item.SubscriptionId == subscriptionId && item.AgentId == agentId)
            .OrderBy(item => item.ZipCodeNormalized)
            .Select(item => item.ZipCodeNormalized)
            .ToListAsync();

        Assert.Equal(["123456789", "54321"], persistedZipCodes);
    }

    [Fact]
    public async Task ReplaceZipCodesAsync_WhenZipCodeIsInvalid_ReturnsInvalidError()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        AgentSalesZipCodeService service = new(dbContext);
        ReplaceAgentSalesZipCodesResult result = await service.ReplaceZipCodesAsync(
            new ReplaceAgentSalesZipCodesInput(agentId, ["ABCDE"]));

        ReplaceAgentSalesZipCodesResult.Failure failure = Assert.IsType<ReplaceAgentSalesZipCodesResult.Failure>(result);
        Assert.Equal("agent_sales_zip_invalid", failure.ErrorCode);
        Assert.Empty(dbContext.AgentSalesZipCodes);
    }

    [Fact]
    public async Task ReplaceZipCodesAsync_WhenNormalizedZipCodesConflict_ReturnsConflictError()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        AgentSalesZipCodeService service = new(dbContext);
        ReplaceAgentSalesZipCodesResult result = await service.ReplaceZipCodesAsync(
            new ReplaceAgentSalesZipCodesInput(agentId, ["12345-6789", "123456789"]));

        ReplaceAgentSalesZipCodesResult.Failure failure = Assert.IsType<ReplaceAgentSalesZipCodesResult.Failure>(result);
        Assert.Equal("agent_sales_zip_conflict", failure.ErrorCode);
    }

    [Fact]
    public async Task ReplaceZipCodesAsync_WhenInputIsEmpty_ClearsZipRestrictions()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        dbContext.AgentSalesZipCodes.Add(new AgentSalesZipCode
        {
            SubscriptionId = subscriptionId,
            AgentId = agentId,
            ZipCodeNormalized = "12345"
        });

        await dbContext.SaveChangesAsync();

        AgentSalesZipCodeService service = new(dbContext);
        ReplaceAgentSalesZipCodesResult result = await service.ReplaceZipCodesAsync(
            new ReplaceAgentSalesZipCodesInput(agentId, []));

        ReplaceAgentSalesZipCodesResult.Success success = Assert.IsType<ReplaceAgentSalesZipCodesResult.Success>(result);
        Assert.False(success.State.HasExplicitZipRestrictions);
        Assert.Empty(success.State.ZipCodesNormalized);
        Assert.Empty(dbContext.AgentSalesZipCodes);
    }

    [Fact]
    public async Task ClearZipCodesAsync_WhenZipCodesExist_RemovesAllZipRestrictions()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        dbContext.AgentSalesZipCodes.Add(new AgentSalesZipCode
        {
            SubscriptionId = subscriptionId,
            AgentId = agentId,
            ZipCodeNormalized = "12345"
        });

        await dbContext.SaveChangesAsync();

        AgentSalesZipCodeService service = new(dbContext);
        ClearAgentSalesZipCodesResult result = await service.ClearZipCodesAsync(
            new ClearAgentSalesZipCodesInput(agentId));

        ClearAgentSalesZipCodesResult.Success success = Assert.IsType<ClearAgentSalesZipCodesResult.Success>(result);
        Assert.False(success.State.HasExplicitZipRestrictions);
        Assert.Empty(success.State.ZipCodesNormalized);
    }

    [Fact]
    public async Task GetStateAsync_WhenZipCodesExist_ReturnsOrderedNormalizedZipCodes()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();
        await SeedAgentAsync(dbContext, subscriptionId, agentId);

        dbContext.AgentSalesZipCodes.AddRange(
            new AgentSalesZipCode
            {
                SubscriptionId = subscriptionId,
                AgentId = agentId,
                ZipCodeNormalized = "54321"
            },
            new AgentSalesZipCode
            {
                SubscriptionId = subscriptionId,
                AgentId = agentId,
                ZipCodeNormalized = "12345"
            });

        await dbContext.SaveChangesAsync();

        AgentSalesZipCodeService service = new(dbContext);
        GetAgentSalesZipCodeStateOperationResult result = await service.GetStateAsync(
            new GetAgentSalesZipCodeStateInput(agentId));

        GetAgentSalesZipCodeStateOperationResult.Success success = Assert.IsType<GetAgentSalesZipCodeStateOperationResult.Success>(result);
        Assert.True(success.State.HasExplicitZipRestrictions);
        Assert.Equal(["12345", "54321"], success.State.ZipCodesNormalized);
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
}
