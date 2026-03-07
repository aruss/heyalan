namespace HeyAlan.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using HeyAlan.Agents;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.TelegramIntegration;
using Telegram.Bot.Exceptions;

public class SubscriptionAgentServiceTests
{
    [Fact]
    public async Task GetAgentsAsync_WhenUserIsNotMember_ReturnsForbiddenFailure()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        SubscriptionAgentService service = CreateService(dbContext);

        GetSubscriptionAgentsResult result = await service.GetAgentsAsync(
            new GetSubscriptionAgentsInput(subscriptionId, userId));

        GetSubscriptionAgentsResult.Failure failure = Assert.IsType<GetSubscriptionAgentsResult.Failure>(result);
        Assert.Equal("subscription_member_required", failure.ErrorCode);
    }

    [Fact]
    public async Task GetAgentsAsync_WhenUserIsMember_ReturnsOnlySubscriptionAgents()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);

        dbContext.Agents.Add(new Agent
        {
            SubscriptionId = subscriptionId,
            Name = "Agent A",
            TwilioPhoneNumber = "+15550000001"
        });

        dbContext.Agents.Add(new Agent
        {
            SubscriptionId = subscriptionId,
            Name = "Agent B"
        });

        dbContext.Agents.Add(new Agent
        {
            SubscriptionId = Guid.NewGuid(),
            Name = "Other Subscription Agent",
            TelegramBotToken = "other-token"
        });

        await dbContext.SaveChangesAsync();

        SubscriptionAgentService service = CreateService(dbContext);
        GetSubscriptionAgentsResult result = await service.GetAgentsAsync(
            new GetSubscriptionAgentsInput(subscriptionId, userId));

        GetSubscriptionAgentsResult.Success success = Assert.IsType<GetSubscriptionAgentsResult.Success>(result);
        Assert.Equal(2, success.Agents.Count);
        Assert.Equal(1, success.Agents.Count(item => item.IsOperationalReady));
    }

    [Fact]
    public async Task UpdateAgentAsync_WhenAllChannelsEmpty_SucceedsAndMarksNotReady()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);

        Agent agent = new()
        {
            SubscriptionId = subscriptionId,
            Name = "Draft Agent",
            Personality = AgentPersonality.Casual,
            TwilioPhoneNumber = "+15550000001",
            TelegramBotToken = "token",
            WhatsappNumber = "+15550000002"
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync();

        SubscriptionAgentService service = CreateService(dbContext);
        UpdateAgentResult result = await service.UpdateAgentAsync(
            new UpdateAgentInput(
                agent.Id,
                userId,
                "Updated Agent",
                AgentPersonality.Business,
                "  prompt  ",
                " ",
                null,
                ""));

        UpdateAgentResult.Success success = Assert.IsType<UpdateAgentResult.Success>(result);
        Assert.False(success.Agent.IsOperationalReady);
        Assert.Null(success.Agent.TwilioPhoneNumber);
        Assert.Null(success.Agent.TelegramBotToken);
        Assert.Null(success.Agent.WhatsappNumber);
        Assert.Equal("prompt", success.Agent.PersonalityPromptRaw);
    }

    [Fact]
    public async Task UpdateAgentAsync_WhenNameMissing_ReturnsValidationFailure()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);

        Agent agent = new()
        {
            SubscriptionId = subscriptionId,
            Name = "Agent",
            Personality = AgentPersonality.Balanced
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync();

        SubscriptionAgentService service = CreateService(dbContext);
        UpdateAgentResult result = await service.UpdateAgentAsync(
            new UpdateAgentInput(
                agent.Id,
                userId,
                " ",
                AgentPersonality.Balanced,
                null,
                null,
                null,
                null));

        UpdateAgentResult.Failure failure = Assert.IsType<UpdateAgentResult.Failure>(result);
        Assert.Equal("agent_name_required", failure.ErrorCode);
    }

    [Fact]
    public async Task UpdateAgentAsync_WhenPersonalityMissing_ReturnsValidationFailure()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);

        Agent agent = new()
        {
            SubscriptionId = subscriptionId,
            Name = "Agent",
            Personality = AgentPersonality.Balanced
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync();

        SubscriptionAgentService service = CreateService(dbContext);
        UpdateAgentResult result = await service.UpdateAgentAsync(
            new UpdateAgentInput(
                agent.Id,
                userId,
                "Agent",
                null,
                null,
                null,
                null,
                null));

        UpdateAgentResult.Failure failure = Assert.IsType<UpdateAgentResult.Failure>(result);
        Assert.Equal("agent_personality_required", failure.ErrorCode);
    }

    [Fact]
    public async Task UpdateAgentAsync_WhenTelegramTokenAlreadyInUse_ReturnsConflictFailure()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);

        Agent first = new()
        {
            SubscriptionId = subscriptionId,
            Name = "First Agent",
            Personality = AgentPersonality.Balanced,
            TelegramBotToken = "in-use-token"
        };

        Agent second = new()
        {
            SubscriptionId = subscriptionId,
            Name = "Second Agent",
            Personality = AgentPersonality.Casual
        };

        dbContext.Agents.Add(first);
        dbContext.Agents.Add(second);
        await dbContext.SaveChangesAsync();

        SubscriptionAgentService service = CreateService(dbContext);
        UpdateAgentResult result = await service.UpdateAgentAsync(
            new UpdateAgentInput(
                second.Id,
                userId,
                "Second Agent Updated",
                AgentPersonality.Business,
                null,
                null,
                "in-use-token",
                null));

        UpdateAgentResult.Failure failure = Assert.IsType<UpdateAgentResult.Failure>(result);
        Assert.Equal("telegram_bot_token_already_in_use", failure.ErrorCode);
    }

    [Fact]
    public async Task UpdateAgentAsync_WhenTelegramWebhookRegistrationFails_RollsBackChangedChannels()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);

        Agent agent = new()
        {
            SubscriptionId = subscriptionId,
            Name = "Agent",
            Personality = AgentPersonality.Casual,
            TwilioPhoneNumber = "+15550000001",
            TelegramBotToken = "old-token",
            WhatsappNumber = "+15550000002"
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync();

        SubscriptionAgentService service = CreateService(
            dbContext,
            new StubTelegramService(StubTelegramBehavior.AlwaysFail));

        UpdateAgentResult result = await service.UpdateAgentAsync(
            new UpdateAgentInput(
                agent.Id,
                userId,
                "Agent Updated",
                AgentPersonality.Balanced,
                null,
                "+15550000003",
                "new-token",
                "+15550000004"));

        UpdateAgentResult.Failure failure = Assert.IsType<UpdateAgentResult.Failure>(result);
        Assert.Equal("telegram_webhook_registration_failed", failure.ErrorCode);

        Agent persisted = await dbContext.Agents.SingleAsync(item => item.Id == agent.Id);
        Assert.Equal("+15550000001", persisted.TwilioPhoneNumber);
        Assert.Equal("old-token", persisted.TelegramBotToken);
        Assert.Equal("+15550000002", persisted.WhatsappNumber);
    }

    [Fact]
    public async Task DeleteAgentAsync_WhenUserIsMember_DeletesAgent()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);

        Agent agent = new()
        {
            SubscriptionId = subscriptionId,
            Name = "Agent",
            Personality = AgentPersonality.Balanced
        };

        dbContext.Agents.Add(agent);
        await dbContext.SaveChangesAsync();

        SubscriptionAgentService service = CreateService(dbContext);
        DeleteAgentResult result = await service.DeleteAgentAsync(new DeleteAgentInput(agent.Id, userId));

        Assert.IsType<DeleteAgentResult.Success>(result);
        Assert.False(await dbContext.Agents.AnyAsync(item => item.Id == agent.Id));
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }

    private static SubscriptionAgentService CreateService(
        MainDataContext dbContext,
        ITelegramService? telegramService = null)
    {
        return new SubscriptionAgentService(
            dbContext,
            telegramService ?? new StubTelegramService(StubTelegramBehavior.Success),
            NullLogger<SubscriptionAgentService>.Instance);
    }

    private static async Task SeedSubscriptionMemberAsync(
        MainDataContext dbContext,
        Guid subscriptionId,
        Guid userId)
    {
        Subscription subscription = new()
        {
            Id = subscriptionId
        };

        ApplicationUser user = new()
        {
            Id = userId,
            DisplayName = "Subscription Member",
            UserName = "member@example.com",
            Email = "member@example.com"
        };

        dbContext.Subscriptions.Add(subscription);
        dbContext.Users.Add(user);
        dbContext.SubscriptionUsers.Add(new SubscriptionUser
        {
            SubscriptionId = subscriptionId,
            UserId = userId,
            Role = SubscriptionUserRole.Member
        });

        await dbContext.SaveChangesAsync();
    }

    private enum StubTelegramBehavior
    {
        Success,
        AlwaysFail,
        InvalidToken
    }

    private sealed class StubTelegramService : ITelegramService
    {
        private readonly StubTelegramBehavior behavior;

        public StubTelegramService(StubTelegramBehavior behavior)
        {
            this.behavior = behavior;
        }

        public Task RegisterWebhookAsync(string botToken, CancellationToken ct = default)
        {
            return this.TryRegisterWebhookAsync(botToken, ct);
        }

        public Task TryRegisterWebhookAsync(string botToken, CancellationToken ct = default)
        {
            if (this.behavior == StubTelegramBehavior.Success)
            {
                return Task.CompletedTask;
            }

            if (this.behavior == StubTelegramBehavior.InvalidToken)
            {
                throw new ApiRequestException("Unauthorized", 401);
            }

            throw new HttpRequestException("Transient transport failure.");
        }

        public async Task<TelegramTokenRegistrationResult> RegisterWebhookIfTokenChangedAsync(
            string? previousBotToken,
            string? nextBotToken,
            CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(nextBotToken) ||
                String.Equals(previousBotToken, nextBotToken, StringComparison.Ordinal))
            {
                return new TelegramTokenRegistrationResult(
                    WasAttempted: false,
                    ErrorCode: null);
            }

            try
            {
                await this.TryRegisterWebhookAsync(nextBotToken, ct);
                return new TelegramTokenRegistrationResult(
                    WasAttempted: true,
                    ErrorCode: null);
            }
            catch (ApiRequestException exception) when (exception.ErrorCode == 401)
            {
                return new TelegramTokenRegistrationResult(
                    WasAttempted: true,
                    ErrorCode: "telegram_bot_token_invalid");
            }
            catch (Exception)
            {
                return new TelegramTokenRegistrationResult(
                    WasAttempted: true,
                    ErrorCode: "telegram_webhook_registration_failed");
            }
        }

        public Task SendMessageAsync(string botToken, long chatId, string text, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
