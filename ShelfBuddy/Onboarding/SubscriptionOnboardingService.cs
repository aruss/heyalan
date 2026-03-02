namespace ShelfBuddy.Onboarding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using ShelfBuddy.Data;
using ShelfBuddy.Data.Entities;
using ShelfBuddy.TelegramIntegration;
using System.Net;
using Telegram.Bot.Exceptions;

public sealed class SubscriptionOnboardingService : ISubscriptionOnboardingService
{
    private static readonly string[] StepOrder =
    [
        "square_connect",
        "profile",
        "channels",
        "invitations",
        "finalize"
    ];

    private static readonly string[] RequiredSquareScopes =
    [
        "ITEMS_READ",
        "CUSTOMERS_READ",
        "CUSTOMERS_WRITE",
        "ORDERS_READ",
        "ORDERS_WRITE",
        "PAYMENTS_WRITE"
    ];
    private const int TelegramWebhookMaxAttempts = 3;

    private readonly MainDataContext dbContext;
    private readonly ITelegramService telegramService;
    private readonly ILogger<SubscriptionOnboardingService> logger;

    public SubscriptionOnboardingService(
        MainDataContext dbContext,
        ITelegramService telegramService,
        ILogger<SubscriptionOnboardingService> logger)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.telegramService = telegramService ?? throw new ArgumentNullException(nameof(telegramService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetSubscriptionOnboardingStateResult> GetStateAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionMemberAsync(subscriptionId, userId, cancellationToken))
        {
            return new GetSubscriptionOnboardingStateResult.Failure("subscription_member_required");
        }

        OnboardingStateResult state = await this.RecomputeStateAsync(subscriptionId, cancellationToken);
        return new GetSubscriptionOnboardingStateResult.Success(state);
    }

    public async Task<CreateSubscriptionOnboardingAgentResult> CreatePrimaryAgentAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionMemberAsync(subscriptionId, userId, cancellationToken))
        {
            return new CreateSubscriptionOnboardingAgentResult.Failure("subscription_member_required");
        }

        SubscriptionOnboardingState onboardingState = await this.GetOrCreateStateAsync(subscriptionId, cancellationToken);

        Agent? existingPrimaryAgent = await this.GetPrimaryAgentAsync(onboardingState, subscriptionId, cancellationToken);
        if (existingPrimaryAgent is not null)
        {
            OnboardingStateResult existingState = await this.RecomputeStateAsync(subscriptionId, cancellationToken);
            return new CreateSubscriptionOnboardingAgentResult.Success(existingPrimaryAgent.Id, existingState);
        }

        Agent agent = new()
        {
            SubscriptionId = subscriptionId,
            Name = "Draft Agent"
        };

        this.dbContext.Agents.Add(agent);
        await this.dbContext.SaveChangesAsync(cancellationToken);

        onboardingState.PrimaryAgentId = agent.Id;
        if (onboardingState.Status == SubscriptionOnboardingStatus.Draft)
        {
            onboardingState.Status = SubscriptionOnboardingStatus.InProgress;
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);

        OnboardingStateResult state = await this.RecomputeStateAsync(subscriptionId, cancellationToken);
        return new CreateSubscriptionOnboardingAgentResult.Success(agent.Id, state);
    }

    public async Task<UpdateSubscriptionOnboardingStepResult> UpdateProfileAsync(
        UpdateSubscriptionOnboardingProfileInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("agent_not_found");
        }

        if (!await this.IsSubscriptionMemberAsync(agent.SubscriptionId, input.UserId, cancellationToken))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("subscription_member_required");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("agent_name_required");
        }

        if (!input.Personality.HasValue)
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("agent_personality_required");
        }

        SubscriptionOnboardingState onboardingState = await this.GetOrCreateStateAsync(agent.SubscriptionId, cancellationToken);
        onboardingState.PrimaryAgentId = agent.Id;

        agent.Name = input.Name.Trim();
        agent.Personality = input.Personality.Value;
        await this.dbContext.SaveChangesAsync(cancellationToken);

        OnboardingStateResult state = await this.RecomputeStateAsync(agent.SubscriptionId, cancellationToken);
        return new UpdateSubscriptionOnboardingStepResult.Success(state);
    }

    public async Task<UpdateSubscriptionOnboardingStepResult> UpdateChannelsAsync(
        UpdateSubscriptionOnboardingChannelsInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("agent_not_found");
        }

        if (!await this.IsSubscriptionMemberAsync(agent.SubscriptionId, input.UserId, cancellationToken))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("subscription_member_required");
        }

        string? twilioPhoneNumber = NormalizeOptionalChannel(input.TwilioPhoneNumber);
        string? telegramBotToken = NormalizeOptionalChannel(input.TelegramBotToken);
        string? whatsappNumber = NormalizeOptionalChannel(input.WhatsappNumber);

        if (string.IsNullOrWhiteSpace(twilioPhoneNumber) &&
            string.IsNullOrWhiteSpace(telegramBotToken) &&
            string.IsNullOrWhiteSpace(whatsappNumber))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("channels_at_least_one_required");
        }

        SubscriptionOnboardingState onboardingState = await this.GetOrCreateStateAsync(agent.SubscriptionId, cancellationToken);
        onboardingState.PrimaryAgentId = agent.Id;

        string? originalTwilioPhoneNumber = agent.TwilioPhoneNumber;
        string? originalTelegramBotToken = agent.TelegramBotToken;
        string? originalWhatsappNumber = agent.WhatsappNumber;

        if (!string.IsNullOrWhiteSpace(telegramBotToken))
        {
            bool isTokenUsedByAnotherAgent = await this.dbContext.Agents
                .AnyAsync(
                    item =>
                        item.Id != agent.Id &&
                        item.TelegramBotToken == telegramBotToken,
                    cancellationToken);

            if (isTokenUsedByAnotherAgent)
            {
                return new UpdateSubscriptionOnboardingStepResult.Failure("telegram_bot_token_already_in_use");
            }
        }

        agent.TwilioPhoneNumber = twilioPhoneNumber;
        agent.TelegramBotToken = telegramBotToken;
        agent.WhatsappNumber = whatsappNumber;
        try
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsTelegramTokenUniqueConstraintViolation(exception))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("telegram_bot_token_already_in_use");
        }

        if (!string.IsNullOrWhiteSpace(telegramBotToken))
        {
            string? webhookRegistrationErrorCode = await this.RegisterTelegramWebhookWithRetryAsync(
                agent.SubscriptionId,
                agent.Id,
                telegramBotToken,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(webhookRegistrationErrorCode))
            {
                this.logger.LogWarning(
                    "Rolling back onboarding channel update because Telegram webhook registration failed for Subscription {SubscriptionId}, Agent {AgentId}. ErrorCode {ErrorCode}.",
                    agent.SubscriptionId,
                    agent.Id,
                    webhookRegistrationErrorCode);

                agent.TwilioPhoneNumber = originalTwilioPhoneNumber;
                agent.TelegramBotToken = originalTelegramBotToken;
                agent.WhatsappNumber = originalWhatsappNumber;
                await this.dbContext.SaveChangesAsync(cancellationToken);

                return new UpdateSubscriptionOnboardingStepResult.Failure(webhookRegistrationErrorCode);
            }
        }

        OnboardingStateResult state = await this.RecomputeStateAsync(agent.SubscriptionId, cancellationToken);
        return new UpdateSubscriptionOnboardingStepResult.Success(state);
    }

    public async Task<UpdateSubscriptionOnboardingStepResult> CompleteInvitationsAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionMemberAsync(subscriptionId, userId, cancellationToken))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("subscription_member_required");
        }

        SubscriptionOnboardingState onboardingState = await this.GetOrCreateStateAsync(subscriptionId, cancellationToken);
        OnboardingComputation computed = await this.ComputeStateAsync(subscriptionId, cancellationToken);

        if (!computed.SquareConnectComplete || !computed.ProfileComplete || !computed.ChannelsComplete)
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("onboarding_invitations_blocked");
        }

        onboardingState.CurrentStep = "finalize";
        if (onboardingState.Status == SubscriptionOnboardingStatus.Draft)
        {
            onboardingState.Status = SubscriptionOnboardingStatus.InProgress;
        }

        await this.dbContext.SaveChangesAsync(cancellationToken);

        OnboardingStateResult state = await this.RecomputeStateAsync(subscriptionId, cancellationToken);
        return new UpdateSubscriptionOnboardingStepResult.Success(state);
    }

    public async Task<UpdateSubscriptionOnboardingStepResult> FinalizeAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionMemberAsync(subscriptionId, userId, cancellationToken))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("subscription_member_required");
        }

        SubscriptionOnboardingState onboardingState = await this.GetOrCreateStateAsync(subscriptionId, cancellationToken);
        OnboardingComputation computedBeforeFinalize = await this.ComputeStateAsync(subscriptionId, cancellationToken);

        if (!computedBeforeFinalize.SquareConnectComplete ||
            !computedBeforeFinalize.ProfileComplete ||
            !computedBeforeFinalize.ChannelsComplete ||
            !computedBeforeFinalize.InvitationsComplete)
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("onboarding_finalize_incomplete");
        }

        onboardingState.Status = SubscriptionOnboardingStatus.Completed;
        onboardingState.CurrentStep = "finalize";
        onboardingState.CompletedAt = DateTime.UtcNow;
        await this.dbContext.SaveChangesAsync(cancellationToken);

        OnboardingStateResult state = await this.RecomputeStateAsync(subscriptionId, cancellationToken);
        return new UpdateSubscriptionOnboardingStepResult.Success(state);
    }

    public async Task<OnboardingStateResult> RecomputeStateAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        SubscriptionOnboardingState onboardingState = await this.GetOrCreateStateAsync(subscriptionId, cancellationToken);
        OnboardingComputation computed = await this.ComputeStateAsync(subscriptionId, cancellationToken);

        onboardingState.PrimaryAgentId = computed.PrimaryAgentId;
        onboardingState.CurrentStep = computed.CurrentStep;
        onboardingState.Status = computed.Status;
        onboardingState.CompletedAt = computed.Status == SubscriptionOnboardingStatus.Completed
            ? onboardingState.CompletedAt
            : null;

        await this.dbContext.SaveChangesAsync(cancellationToken);
        return BuildStateResult(computed);
    }

    private async Task<bool> IsSubscriptionMemberAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (subscriptionId == Guid.Empty || userId == Guid.Empty)
        {
            return false;
        }

        bool isMember = await this.dbContext.SubscriptionUsers
            .AnyAsync(
                membership =>
                    membership.SubscriptionId == subscriptionId &&
                    membership.UserId == userId,
                cancellationToken);

        return isMember;
    }

    private async Task<SubscriptionOnboardingState> GetOrCreateStateAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        SubscriptionOnboardingState? onboardingState = await this.dbContext.SubscriptionOnboardingStates
            .SingleOrDefaultAsync(item => item.SubscriptionId == subscriptionId, cancellationToken);

        if (onboardingState is not null)
        {
            return onboardingState;
        }

        onboardingState = new SubscriptionOnboardingState
        {
            SubscriptionId = subscriptionId,
            Status = SubscriptionOnboardingStatus.Draft,
            CurrentStep = "square_connect",
            StartedAt = DateTime.UtcNow
        };

        this.dbContext.SubscriptionOnboardingStates.Add(onboardingState);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        return onboardingState;
    }

    private async Task<Agent?> GetPrimaryAgentAsync(
        SubscriptionOnboardingState onboardingState,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        if (!onboardingState.PrimaryAgentId.HasValue)
        {
            return null;
        }

        Agent? primaryAgent = await this.dbContext.Agents
            .SingleOrDefaultAsync(
                item =>
                    item.Id == onboardingState.PrimaryAgentId.Value &&
                    item.SubscriptionId == subscriptionId,
                cancellationToken);

        return primaryAgent;
    }

    private async Task<OnboardingComputation> ComputeStateAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        SubscriptionOnboardingState onboardingState = await this.GetOrCreateStateAsync(subscriptionId, cancellationToken);
        SubscriptionSquareConnection? connection = await this.dbContext.SubscriptionSquareConnections
            .SingleOrDefaultAsync(item => item.SubscriptionId == subscriptionId, cancellationToken);

        Agent? primaryAgent = await this.GetPrimaryAgentAsync(onboardingState, subscriptionId, cancellationToken);
        Guid? primaryAgentId = primaryAgent?.Id;

        bool squareConnectComplete = IsSquareConnectComplete(connection);
        bool profileComplete = IsProfileComplete(primaryAgent);
        bool channelsComplete = IsChannelsComplete(primaryAgent);
        bool invitationsComplete = onboardingState.Status == SubscriptionOnboardingStatus.Completed ||
            string.Equals(onboardingState.CurrentStep, "finalize", StringComparison.Ordinal);

        bool canFinalize = squareConnectComplete && profileComplete && channelsComplete && invitationsComplete;
        bool finalizeComplete =
            onboardingState.Status == SubscriptionOnboardingStatus.Completed &&
            onboardingState.CompletedAt.HasValue &&
            canFinalize;

        string currentStep = finalizeComplete
            ? "finalize"
            : ResolveEarliestIncompleteStep(
                squareConnectComplete,
                profileComplete,
                channelsComplete,
                invitationsComplete);

        SubscriptionOnboardingStatus status = ResolveStatus(
            squareConnectComplete,
            profileComplete,
            channelsComplete,
            invitationsComplete,
            finalizeComplete);

        return new OnboardingComputation(
            status,
            currentStep,
            primaryAgentId,
            squareConnectComplete,
            profileComplete,
            channelsComplete,
            invitationsComplete,
            canFinalize,
            finalizeComplete);
    }

    private static bool IsSquareConnectComplete(SubscriptionSquareConnection? connection)
    {
        if (connection is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(connection.EncryptedAccessToken) ||
            string.IsNullOrWhiteSpace(connection.EncryptedRefreshToken))
        {
            return false;
        }

        HashSet<string> grantedScopes = connection.Scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);

        return RequiredSquareScopes.All(grantedScopes.Contains);
    }

    private static bool IsProfileComplete(Agent? agent)
    {
        return agent is not null &&
            !string.IsNullOrWhiteSpace(agent.Name) &&
            agent.Personality.HasValue;
    }

    private static bool IsChannelsComplete(Agent? agent)
    {
        return agent is not null &&
            (
                !string.IsNullOrWhiteSpace(agent.TwilioPhoneNumber) ||
                !string.IsNullOrWhiteSpace(agent.TelegramBotToken) ||
                !string.IsNullOrWhiteSpace(agent.WhatsappNumber)
            );
    }

    private static string? NormalizeOptionalChannel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string ResolveEarliestIncompleteStep(
        bool squareConnectComplete,
        bool profileComplete,
        bool channelsComplete,
        bool invitationsComplete)
    {
        if (!squareConnectComplete)
        {
            return "square_connect";
        }

        if (!profileComplete)
        {
            return "profile";
        }

        if (!channelsComplete)
        {
            return "channels";
        }

        if (!invitationsComplete)
        {
            return "invitations";
        }

        return "finalize";
    }

    private static SubscriptionOnboardingStatus ResolveStatus(
        bool squareConnectComplete,
        bool profileComplete,
        bool channelsComplete,
        bool invitationsComplete,
        bool finalizeComplete)
    {
        if (finalizeComplete)
        {
            return SubscriptionOnboardingStatus.Completed;
        }

        if (!squareConnectComplete &&
            !profileComplete &&
            !channelsComplete &&
            !invitationsComplete)
        {
            return SubscriptionOnboardingStatus.Draft;
        }

        return SubscriptionOnboardingStatus.InProgress;
    }

    private static OnboardingStateResult BuildStateResult(OnboardingComputation computed)
    {
        bool[] completionFlags =
        [
            computed.SquareConnectComplete,
            computed.ProfileComplete,
            computed.ChannelsComplete,
            computed.InvitationsComplete,
            computed.FinalizeComplete
        ];

        int currentStepIndex = Array.IndexOf(StepOrder, computed.CurrentStep);
        if (currentStepIndex < 0)
        {
            currentStepIndex = 0;
        }

        List<OnboardingStepState> stepStates = new(StepOrder.Length);
        for (int index = 0; index < StepOrder.Length; index++)
        {
            string status;
            if (completionFlags[index])
            {
                status = "completed";
            }
            else if (index == currentStepIndex)
            {
                status = "in_progress";
            }
            else if (index > currentStepIndex)
            {
                status = "blocked";
            }
            else
            {
                status = "not_started";
            }

            stepStates.Add(new OnboardingStepState(StepOrder[index], status));
        }

        return new OnboardingStateResult(
            computed.Status.ToString(),
            computed.CurrentStep,
            [.. stepStates],
            computed.PrimaryAgentId,
            computed.CanFinalize);
    }

    private async Task<string?> RegisterTelegramWebhookWithRetryAsync(
        Guid subscriptionId,
        Guid agentId,
        string telegramBotToken,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= TelegramWebhookMaxAttempts; attempt++)
        {
            this.logger.LogInformation(
                "Registering Telegram webhook for Subscription {SubscriptionId}, Agent {AgentId}. Attempt {Attempt} of {MaxAttempts}.",
                subscriptionId,
                agentId,
                attempt,
                TelegramWebhookMaxAttempts);

            try
            {
                await this.telegramService.RegisterWebhookAsync(telegramBotToken, cancellationToken);

                this.logger.LogInformation(
                    "Telegram webhook registration succeeded for Subscription {SubscriptionId}, Agent {AgentId} on attempt {Attempt}.",
                    subscriptionId,
                    agentId,
                    attempt);

                return null;
            }
            catch (ApiRequestException exception) when (!IsTransientTelegramApiException(exception))
            {
                string nonTransientErrorCode = ResolveNonTransientTelegramErrorCode(exception);

                this.logger.LogWarning(
                    exception,
                    "Telegram webhook registration failed with non-transient API error for Subscription {SubscriptionId}, Agent {AgentId}. StatusCode {StatusCode}. ErrorCode {ErrorCode}.",
                    subscriptionId,
                    agentId,
                    exception.ErrorCode,
                    nonTransientErrorCode);

                return nonTransientErrorCode;
            }
            catch (Exception exception) when (attempt < TelegramWebhookMaxAttempts && IsTransientTelegramException(exception))
            {
                TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

                this.logger.LogWarning(
                    exception,
                    "Telegram webhook registration transient failure for Subscription {SubscriptionId}, Agent {AgentId}. Attempt {Attempt} failed, retrying in {DelaySeconds} second(s).",
                    subscriptionId,
                    agentId,
                    attempt,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception exception)
            {
                this.logger.LogWarning(
                    exception,
                    "Telegram webhook registration failed for Subscription {SubscriptionId}, Agent {AgentId} after attempt {Attempt}.",
                    subscriptionId,
                    agentId,
                    attempt);

                return "telegram_webhook_registration_failed";
            }
        }

        return "telegram_webhook_registration_failed";
    }

    private static bool IsTelegramTokenUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return false;
        }

        return string.Equals(postgresException.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(postgresException.ConstraintName) &&
            postgresException.ConstraintName.Contains("telegram_bot_token", StringComparison.Ordinal);
    }

    private static bool IsTransientTelegramApiException(ApiRequestException exception)
    {
        return exception.ErrorCode == (int)HttpStatusCode.TooManyRequests ||
            exception.ErrorCode >= 500;
    }

    private static bool IsTransientTelegramException(Exception exception)
    {
        if (exception is ApiRequestException apiRequestException)
        {
            return IsTransientTelegramApiException(apiRequestException);
        }

        if (exception is RequestException requestException)
        {
            return requestException.InnerException is HttpRequestException;
        }

        return exception is HttpRequestException;
    }

    private static string ResolveNonTransientTelegramErrorCode(ApiRequestException exception)
    {
        if (exception.ErrorCode == (int)HttpStatusCode.Unauthorized)
        {
            return "telegram_bot_token_invalid";
        }

        return "telegram_webhook_registration_failed";
    }

    private sealed record OnboardingComputation(
        SubscriptionOnboardingStatus Status,
        string CurrentStep,
        Guid? PrimaryAgentId,
        bool SquareConnectComplete,
        bool ProfileComplete,
        bool ChannelsComplete,
        bool InvitationsComplete,
        bool CanFinalize,
        bool FinalizeComplete);
}
