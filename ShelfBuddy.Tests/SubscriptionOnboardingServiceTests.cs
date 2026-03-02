namespace ShelfBuddy.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ShelfBuddy;
using ShelfBuddy.Data;
using ShelfBuddy.Data.Entities;
using ShelfBuddy.Onboarding;
using ShelfBuddy.TelegramIntegration;
using Telegram.Bot.Exceptions;

public class SubscriptionOnboardingServiceTests
{
    [Fact]
    public async Task GetStateAsync_WhenDraft_ReturnsSquareConnectCurrentStep()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);

        SubscriptionOnboardingService service = CreateService(dbContext);

        GetSubscriptionOnboardingStateResult result = await service.GetStateAsync(subscriptionId, userId);

        GetSubscriptionOnboardingStateResult.Success success =
            Assert.IsType<GetSubscriptionOnboardingStateResult.Success>(result);
        Assert.Equal("Draft", success.State.Status);
        Assert.Equal("square_connect", success.State.CurrentStep);
        Assert.False(success.State.CanFinalize);
    }

    [Fact]
    public async Task UpdateChannelsAsync_WhenOnlyTelegramProvided_Succeeds()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);
        SeedConnectedSquare(dbContext, subscriptionId, userId);
        await dbContext.SaveChangesAsync();

        SubscriptionOnboardingService service = CreateService(dbContext);
        CreateSubscriptionOnboardingAgentResult createResult = await service.CreatePrimaryAgentAsync(subscriptionId, userId);
        CreateSubscriptionOnboardingAgentResult.Success createSuccess =
            Assert.IsType<CreateSubscriptionOnboardingAgentResult.Success>(createResult);

        UpdateSubscriptionOnboardingStepResult result = await service.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                createSuccess.AgentId,
                userId,
                null,
                "telegram-token",
                null));

        UpdateSubscriptionOnboardingStepResult.Success success =
            Assert.IsType<UpdateSubscriptionOnboardingStepResult.Success>(result);
        Assert.Contains(success.State.Steps, item => item.Step == "channels" && item.Status == "completed");
    }

    [Fact]
    public async Task UpdateChannelsAsync_WhenAllChannelsMissing_ReturnsValidationError()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);
        SeedConnectedSquare(dbContext, subscriptionId, userId);
        await dbContext.SaveChangesAsync();

        SubscriptionOnboardingService service = CreateService(dbContext);
        CreateSubscriptionOnboardingAgentResult createResult = await service.CreatePrimaryAgentAsync(subscriptionId, userId);
        CreateSubscriptionOnboardingAgentResult.Success createSuccess =
            Assert.IsType<CreateSubscriptionOnboardingAgentResult.Success>(createResult);

        UpdateSubscriptionOnboardingStepResult result = await service.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                createSuccess.AgentId,
                userId,
                "   ",
                null,
                ""));

        UpdateSubscriptionOnboardingStepResult.Failure failure =
            Assert.IsType<UpdateSubscriptionOnboardingStepResult.Failure>(result);
        Assert.Equal("channels_at_least_one_required", failure.ErrorCode);
    }

    [Fact]
    public async Task FinalizeAsync_WhenAllStepsCompleted_ReturnsCompletedState()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);
        SeedConnectedSquare(dbContext, subscriptionId, userId);
        await dbContext.SaveChangesAsync();

        SubscriptionOnboardingService service = CreateService(dbContext);

        CreateSubscriptionOnboardingAgentResult createResult = await service.CreatePrimaryAgentAsync(subscriptionId, userId);
        CreateSubscriptionOnboardingAgentResult.Success createSuccess =
            Assert.IsType<CreateSubscriptionOnboardingAgentResult.Success>(createResult);

        UpdateSubscriptionOnboardingStepResult profileResult = await service.UpdateProfileAsync(
            new UpdateSubscriptionOnboardingProfileInput(
                createSuccess.AgentId,
                userId,
                "Shelf Buddy",
                AgentPersonality.Balanced));
        Assert.IsType<UpdateSubscriptionOnboardingStepResult.Success>(profileResult);

        UpdateSubscriptionOnboardingStepResult channelsResult = await service.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                createSuccess.AgentId,
                userId,
                "+15550000001",
                "telegram-token",
                "+15550000002"));
        Assert.IsType<UpdateSubscriptionOnboardingStepResult.Success>(channelsResult);

        UpdateSubscriptionOnboardingStepResult invitationResult = await service.CompleteInvitationsAsync(subscriptionId, userId);
        Assert.IsType<UpdateSubscriptionOnboardingStepResult.Success>(invitationResult);

        UpdateSubscriptionOnboardingStepResult finalizeResult = await service.FinalizeAsync(subscriptionId, userId);
        UpdateSubscriptionOnboardingStepResult.Success finalizeSuccess =
            Assert.IsType<UpdateSubscriptionOnboardingStepResult.Success>(finalizeResult);

        Assert.Equal("Completed", finalizeSuccess.State.Status);
        Assert.Equal("finalize", finalizeSuccess.State.CurrentStep);
        Assert.True(finalizeSuccess.State.CanFinalize);
    }

    [Fact]
    public async Task FinalizeAsync_WhenInvitationsNotCompleted_ReturnsValidationError()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);
        SeedConnectedSquare(dbContext, subscriptionId, userId);
        await dbContext.SaveChangesAsync();

        SubscriptionOnboardingService service = CreateService(dbContext);
        CreateSubscriptionOnboardingAgentResult createResult = await service.CreatePrimaryAgentAsync(subscriptionId, userId);
        CreateSubscriptionOnboardingAgentResult.Success createSuccess =
            Assert.IsType<CreateSubscriptionOnboardingAgentResult.Success>(createResult);

        await service.UpdateProfileAsync(
            new UpdateSubscriptionOnboardingProfileInput(
                createSuccess.AgentId,
                userId,
                "Shelf Buddy",
                AgentPersonality.Business));
        await service.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                createSuccess.AgentId,
                userId,
                "+15550000001",
                "telegram-token",
                "+15550000002"));

        UpdateSubscriptionOnboardingStepResult finalizeResult = await service.FinalizeAsync(subscriptionId, userId);
        UpdateSubscriptionOnboardingStepResult.Failure failure =
            Assert.IsType<UpdateSubscriptionOnboardingStepResult.Failure>(finalizeResult);
        Assert.Equal("onboarding_finalize_incomplete", failure.ErrorCode);
    }

    [Fact]
    public async Task RecomputeStateAsync_WhenAllChannelsInvalidated_FallsBackToInProgress()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);
        SeedConnectedSquare(dbContext, subscriptionId, userId);
        await dbContext.SaveChangesAsync();

        SubscriptionOnboardingService service = CreateService(dbContext);
        CreateSubscriptionOnboardingAgentResult createResult = await service.CreatePrimaryAgentAsync(subscriptionId, userId);
        CreateSubscriptionOnboardingAgentResult.Success createSuccess =
            Assert.IsType<CreateSubscriptionOnboardingAgentResult.Success>(createResult);

        await service.UpdateProfileAsync(
            new UpdateSubscriptionOnboardingProfileInput(
                createSuccess.AgentId,
                userId,
                "Shelf Buddy",
                AgentPersonality.Casual));
        await service.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                createSuccess.AgentId,
                userId,
                "+15550000001",
                "telegram-token",
                "+15550000002"));
        await service.CompleteInvitationsAsync(subscriptionId, userId);
        await service.FinalizeAsync(subscriptionId, userId);

        Agent agent = await dbContext.Agents.SingleAsync(item => item.Id == createSuccess.AgentId);
        agent.TwilioPhoneNumber = null;
        agent.TelegramBotToken = null;
        agent.WhatsappNumber = null;
        await dbContext.SaveChangesAsync();

        OnboardingStateResult state = await service.RecomputeStateAsync(subscriptionId);

        Assert.Equal("InProgress", state.Status);
        Assert.Equal("channels", state.CurrentStep);
    }

    [Fact]
    public async Task UpdateChannelsAsync_WhenTelegramWebhookRegistrationFails_RollsBackAndReturnsFailure()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);
        SeedConnectedSquare(dbContext, subscriptionId, userId);
        await dbContext.SaveChangesAsync();

        SubscriptionOnboardingService service = CreateService(
            dbContext,
            new StubTelegramService(StubTelegramBehavior.AlwaysFail));

        CreateSubscriptionOnboardingAgentResult createResult = await service.CreatePrimaryAgentAsync(subscriptionId, userId);
        CreateSubscriptionOnboardingAgentResult.Success createSuccess =
            Assert.IsType<CreateSubscriptionOnboardingAgentResult.Success>(createResult);

        UpdateSubscriptionOnboardingStepResult result = await service.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                createSuccess.AgentId,
                userId,
                null,
                "telegram-token",
                null));

        UpdateSubscriptionOnboardingStepResult.Failure failure =
            Assert.IsType<UpdateSubscriptionOnboardingStepResult.Failure>(result);
        Assert.Equal("telegram_webhook_registration_failed", failure.ErrorCode);

        Agent agent = await dbContext.Agents.SingleAsync(item => item.Id == createSuccess.AgentId);
        Assert.Null(agent.TelegramBotToken);
    }

    [Fact]
    public async Task UpdateChannelsAsync_WhenTelegramTokenIsInvalid_ReturnsInvalidTokenError()
    {
        MainDataContext dbContext = CreateContext();
        Guid subscriptionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, subscriptionId, userId);
        SeedConnectedSquare(dbContext, subscriptionId, userId);
        await dbContext.SaveChangesAsync();

        SubscriptionOnboardingService service = CreateService(
            dbContext,
            new StubTelegramService(StubTelegramBehavior.InvalidToken));

        CreateSubscriptionOnboardingAgentResult createResult = await service.CreatePrimaryAgentAsync(subscriptionId, userId);
        CreateSubscriptionOnboardingAgentResult.Success createSuccess =
            Assert.IsType<CreateSubscriptionOnboardingAgentResult.Success>(createResult);

        UpdateSubscriptionOnboardingStepResult result = await service.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                createSuccess.AgentId,
                userId,
                null,
                "telegram-token",
                null));

        UpdateSubscriptionOnboardingStepResult.Failure failure =
            Assert.IsType<UpdateSubscriptionOnboardingStepResult.Failure>(result);
        Assert.Equal("telegram_bot_token_invalid", failure.ErrorCode);
    }

    [Fact]
    public async Task UpdateChannelsAsync_WhenTelegramTokenAlreadyInUse_ReturnsDuplicateTokenError()
    {
        MainDataContext dbContext = CreateContext();

        Guid firstSubscriptionId = Guid.NewGuid();
        Guid firstUserId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, firstSubscriptionId, firstUserId);
        SeedConnectedSquare(dbContext, firstSubscriptionId, firstUserId);

        Guid secondSubscriptionId = Guid.NewGuid();
        Guid secondUserId = Guid.NewGuid();
        await SeedSubscriptionMemberAsync(dbContext, secondSubscriptionId, secondUserId);
        SeedConnectedSquare(dbContext, secondSubscriptionId, secondUserId);

        await dbContext.SaveChangesAsync();

        SubscriptionOnboardingService service = CreateService(dbContext);

        CreateSubscriptionOnboardingAgentResult firstCreateResult = await service.CreatePrimaryAgentAsync(firstSubscriptionId, firstUserId);
        CreateSubscriptionOnboardingAgentResult.Success firstCreateSuccess =
            Assert.IsType<CreateSubscriptionOnboardingAgentResult.Success>(firstCreateResult);

        UpdateSubscriptionOnboardingStepResult firstChannelsResult = await service.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                firstCreateSuccess.AgentId,
                firstUserId,
                null,
                "shared-telegram-token",
                null));

        Assert.IsType<UpdateSubscriptionOnboardingStepResult.Success>(firstChannelsResult);

        CreateSubscriptionOnboardingAgentResult secondCreateResult = await service.CreatePrimaryAgentAsync(secondSubscriptionId, secondUserId);
        CreateSubscriptionOnboardingAgentResult.Success secondCreateSuccess =
            Assert.IsType<CreateSubscriptionOnboardingAgentResult.Success>(secondCreateResult);

        UpdateSubscriptionOnboardingStepResult secondChannelsResult = await service.UpdateChannelsAsync(
            new UpdateSubscriptionOnboardingChannelsInput(
                secondCreateSuccess.AgentId,
                secondUserId,
                null,
                "shared-telegram-token",
                null));

        UpdateSubscriptionOnboardingStepResult.Failure failure =
            Assert.IsType<UpdateSubscriptionOnboardingStepResult.Failure>(secondChannelsResult);
        Assert.Equal("telegram_bot_token_already_in_use", failure.ErrorCode);
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }

    private static SubscriptionOnboardingService CreateService(
        MainDataContext dbContext,
        ITelegramService? telegramService = null)
    {
        return new SubscriptionOnboardingService(
            dbContext,
            telegramService ?? new StubTelegramService(StubTelegramBehavior.Success),
            NullLogger<SubscriptionOnboardingService>.Instance);
    }

    private static async Task SeedSubscriptionMemberAsync(MainDataContext dbContext, Guid subscriptionId, Guid userId)
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

    private static void SeedConnectedSquare(MainDataContext dbContext, Guid subscriptionId, Guid connectedByUserId)
    {
        dbContext.SubscriptionSquareConnections.Add(new SubscriptionSquareConnection
        {
            SubscriptionId = subscriptionId,
            SquareMerchantId = "merchant-1",
            EncryptedAccessToken = "encrypted-access",
            EncryptedRefreshToken = "encrypted-refresh",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            Scopes = "ITEMS_READ CUSTOMERS_READ CUSTOMERS_WRITE ORDERS_READ ORDERS_WRITE PAYMENTS_WRITE",
            ConnectedByUserId = connectedByUserId
        });
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

        public Task SendMessageAsync(string botToken, long chatId, string text, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
