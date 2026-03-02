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
    static SubscriptionOnboardingService()
    {
        ValidateStepDefinitions();
    }

    private static readonly OnboardingStepDefinition[] StepDefinitions =
    [
        new("square_connect", true, []),
        new("profile", false, []),
        new("channels", true, []),
        new("invitations", true, ["square_connect"]),
        new("finalize", false, [])
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
    private const string StepStatusNotStarted = "not_started";
    private const string StepStatusInProgress = "in_progress";
    private const string StepStatusCompleted = "completed";
    private const string StepStatusSkipped = "skipped";
    private const string StepStatusBlocked = "blocked";

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
        OnboardingStepComputation? invitationsStep = computed.StepStates.SingleOrDefault(item =>
            string.Equals(item.Definition.Name, "invitations", StringComparison.Ordinal));

        if (invitationsStep is null ||
            string.Equals(invitationsStep.Status, StepStatusBlocked, StringComparison.Ordinal))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("onboarding_invitations_blocked");
        }

        SubscriptionOnboardingStepState invitationState = await this.GetOrCreateStepStateAsync(subscriptionId, "invitations", cancellationToken);
        invitationState.Status = StepStatusCompleted;
        invitationState.CompletedAt = DateTime.UtcNow;
        invitationState.SkippedAt = null;

        onboardingState.CurrentStep = computed.CurrentStep;
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

        if (!computedBeforeFinalize.CanFinalize)
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("onboarding_finalize_incomplete");
        }

        SubscriptionOnboardingStepState finalizeState = await this.GetOrCreateStepStateAsync(subscriptionId, "finalize", cancellationToken);
        finalizeState.Status = StepStatusCompleted;
        finalizeState.CompletedAt = DateTime.UtcNow;
        finalizeState.SkippedAt = null;

        onboardingState.Status = SubscriptionOnboardingStatus.Completed;
        onboardingState.CurrentStep = "finalize";
        onboardingState.CompletedAt = DateTime.UtcNow;
        await this.dbContext.SaveChangesAsync(cancellationToken);

        OnboardingStateResult state = await this.RecomputeStateAsync(subscriptionId, cancellationToken);
        return new UpdateSubscriptionOnboardingStepResult.Success(state);
    }

    public async Task<UpdateSubscriptionOnboardingStepResult> SkipStepAsync(
        Guid subscriptionId,
        Guid userId,
        string step,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionMemberAsync(subscriptionId, userId, cancellationToken))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("subscription_member_required");
        }

        if (string.IsNullOrWhiteSpace(step))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("step_not_found");
        }

        OnboardingStepDefinition? definition = GetStepDefinition(step.Trim());
        if (definition is null)
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("step_not_found");
        }

        if (!definition.IsSkippable)
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("step_not_skippable");
        }

        SubscriptionOnboardingState onboardingState = await this.GetOrCreateStateAsync(subscriptionId, cancellationToken);
        SubscriptionOnboardingStepState stepState = await this.GetOrCreateStepStateAsync(subscriptionId, definition.Name, cancellationToken);
        stepState.Status = StepStatusSkipped;
        stepState.SkippedAt = DateTime.UtcNow;
        stepState.CompletedAt = null;

        if (onboardingState.Status == SubscriptionOnboardingStatus.Draft)
        {
            onboardingState.Status = SubscriptionOnboardingStatus.InProgress;
        }

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
            ? onboardingState.CompletedAt ?? DateTime.UtcNow
            : null;

        await this.UpsertComputedStepStatesAsync(subscriptionId, computed.StepStates, cancellationToken);

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

    private async Task<SubscriptionOnboardingStepState> GetOrCreateStepStateAsync(
        Guid subscriptionId,
        string step,
        CancellationToken cancellationToken)
    {
        SubscriptionOnboardingStepState? existingState = await this.dbContext.SubscriptionOnboardingStepStates
            .SingleOrDefaultAsync(
                item =>
                    item.SubscriptionId == subscriptionId &&
                    item.Step == step,
                cancellationToken);

        if (existingState is not null)
        {
            return existingState;
        }

        SubscriptionOnboardingStepState stepState = new()
        {
            SubscriptionId = subscriptionId,
            Step = step,
            Status = StepStatusNotStarted
        };

        this.dbContext.SubscriptionOnboardingStepStates.Add(stepState);
        return stepState;
    }

    private async Task UpsertComputedStepStatesAsync(
        Guid subscriptionId,
        IReadOnlyCollection<OnboardingStepComputation> stepStates,
        CancellationToken cancellationToken)
    {
        List<SubscriptionOnboardingStepState> existingStepStates = await this.dbContext.SubscriptionOnboardingStepStates
            .Where(item => item.SubscriptionId == subscriptionId)
            .ToListAsync(cancellationToken);

        Dictionary<string, SubscriptionOnboardingStepState> existingByStep = existingStepStates
            .ToDictionary(item => item.Step, StringComparer.Ordinal);

        foreach (OnboardingStepComputation computedStep in stepStates)
        {
            if (!existingByStep.TryGetValue(computedStep.Definition.Name, out SubscriptionOnboardingStepState? persistedStep))
            {
                persistedStep = new SubscriptionOnboardingStepState
                {
                    SubscriptionId = subscriptionId,
                    Step = computedStep.Definition.Name
                };

                this.dbContext.SubscriptionOnboardingStepStates.Add(persistedStep);
                existingByStep[persistedStep.Step] = persistedStep;
            }

            persistedStep.Status = computedStep.Status;

            if (string.Equals(computedStep.Status, StepStatusCompleted, StringComparison.Ordinal))
            {
                persistedStep.CompletedAt ??= DateTime.UtcNow;
                persistedStep.SkippedAt = null;
            }
            else if (string.Equals(computedStep.Status, StepStatusSkipped, StringComparison.Ordinal))
            {
                persistedStep.SkippedAt ??= DateTime.UtcNow;
                persistedStep.CompletedAt = null;
            }
            else
            {
                persistedStep.CompletedAt = null;
                persistedStep.SkippedAt = null;
            }
        }
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
        List<SubscriptionOnboardingStepState> persistedStepStates = await this.dbContext.SubscriptionOnboardingStepStates
            .Where(item => item.SubscriptionId == subscriptionId)
            .ToListAsync(cancellationToken);

        Dictionary<string, SubscriptionOnboardingStepState> persistedByStep = persistedStepStates
            .ToDictionary(item => item.Step, StringComparer.Ordinal);

        Dictionary<string, bool> completedByStep = new(StringComparer.Ordinal)
        {
            ["square_connect"] = IsSquareConnectComplete(connection),
            ["profile"] = IsProfileComplete(primaryAgent),
            ["channels"] = IsChannelsComplete(primaryAgent),
            ["invitations"] = IsPersistedCompleted(persistedByStep, "invitations"),
            ["finalize"] = IsPersistedCompleted(persistedByStep, "finalize") &&
                onboardingState.Status == SubscriptionOnboardingStatus.Completed &&
                onboardingState.CompletedAt.HasValue
        };

        List<OnboardingStepComputation> computedSteps = [];
        foreach (OnboardingStepDefinition definition in StepDefinitions)
        {
            bool isCompleted = completedByStep.TryGetValue(definition.Name, out bool completionValue) &&
                completionValue;

            if (isCompleted)
            {
                computedSteps.Add(new OnboardingStepComputation(definition, StepStatusCompleted));
                continue;
            }

            if (IsPersistedSkipped(persistedByStep, definition.Name))
            {
                computedSteps.Add(new OnboardingStepComputation(definition, StepStatusSkipped));
                continue;
            }

            bool hasSkippedDependency = definition.DependsOn.Any(dependency =>
            {
                OnboardingStepComputation? dependencyStep = computedSteps.SingleOrDefault(item =>
                    string.Equals(item.Definition.Name, dependency, StringComparison.Ordinal));

                return dependencyStep is not null &&
                    string.Equals(dependencyStep.Status, StepStatusSkipped, StringComparison.Ordinal);
            });

            if (hasSkippedDependency)
            {
                computedSteps.Add(new OnboardingStepComputation(definition, StepStatusSkipped));
                continue;
            }

            bool hasBlockedDependency = definition.DependsOn.Any(dependency =>
            {
                OnboardingStepComputation? dependencyStep = computedSteps.SingleOrDefault(item =>
                    string.Equals(item.Definition.Name, dependency, StringComparison.Ordinal));

                return dependencyStep is null ||
                    !string.Equals(dependencyStep.Status, StepStatusCompleted, StringComparison.Ordinal);
            });

            if (hasBlockedDependency)
            {
                computedSteps.Add(new OnboardingStepComputation(definition, StepStatusBlocked));
                continue;
            }

            computedSteps.Add(new OnboardingStepComputation(definition, StepStatusNotStarted));
        }

        OnboardingStepComputation? currentStepState = computedSteps.FirstOrDefault(item =>
            string.Equals(item.Status, StepStatusNotStarted, StringComparison.Ordinal));

        string currentStep = currentStepState?.Definition.Name ?? "finalize";

        List<OnboardingStepComputation> normalizedStepStates = [];
        foreach (OnboardingStepComputation stepState in computedSteps)
        {
            if (currentStepState is not null &&
                string.Equals(stepState.Definition.Name, currentStepState.Definition.Name, StringComparison.Ordinal))
            {
                normalizedStepStates.Add(new OnboardingStepComputation(stepState.Definition, StepStatusInProgress));
                continue;
            }

            normalizedStepStates.Add(stepState);
        }

        bool hasIncompleteRequiredSteps = normalizedStepStates.Any(item =>
            !item.Definition.IsSkippable &&
            !string.Equals(item.Definition.Name, "finalize", StringComparison.Ordinal) &&
            !string.Equals(item.Status, StepStatusCompleted, StringComparison.Ordinal));

        bool canFinalize = !hasIncompleteRequiredSteps;
        bool finalizeComplete = normalizedStepStates.Any(item =>
            string.Equals(item.Definition.Name, "finalize", StringComparison.Ordinal) &&
            string.Equals(item.Status, StepStatusCompleted, StringComparison.Ordinal));

        SubscriptionOnboardingStatus status = ResolveStatus(normalizedStepStates, finalizeComplete);

        return new OnboardingComputation(
            status,
            finalizeComplete ? "finalize" : currentStep,
            primaryAgentId,
            canFinalize,
            finalizeComplete,
            normalizedStepStates);
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

    private static bool IsPersistedCompleted(
        IReadOnlyDictionary<string, SubscriptionOnboardingStepState> persistedByStep,
        string step)
    {
        if (!persistedByStep.TryGetValue(step, out SubscriptionOnboardingStepState? stepState))
        {
            return false;
        }

        return string.Equals(stepState.Status, StepStatusCompleted, StringComparison.Ordinal);
    }

    private static bool IsPersistedSkipped(
        IReadOnlyDictionary<string, SubscriptionOnboardingStepState> persistedByStep,
        string step)
    {
        if (!persistedByStep.TryGetValue(step, out SubscriptionOnboardingStepState? stepState))
        {
            return false;
        }

        return string.Equals(stepState.Status, StepStatusSkipped, StringComparison.Ordinal);
    }

    private static OnboardingStepDefinition? GetStepDefinition(string step)
    {
        return StepDefinitions.SingleOrDefault(item =>
            string.Equals(item.Name, step, StringComparison.Ordinal));
    }

    private static void ValidateStepDefinitions()
    {
        HashSet<string> seenNames = new(StringComparer.Ordinal);
        foreach (OnboardingStepDefinition definition in StepDefinitions)
        {
            if (!seenNames.Add(definition.Name))
            {
                throw new InvalidOperationException($"Duplicate onboarding step definition: {definition.Name}.");
            }
        }

        foreach (OnboardingStepDefinition definition in StepDefinitions)
        {
            foreach (string dependency in definition.DependsOn)
            {
                if (!seenNames.Contains(dependency))
                {
                    throw new InvalidOperationException(
                        $"Onboarding step '{definition.Name}' has unknown dependency '{dependency}'.");
                }
            }
        }
    }

    private static SubscriptionOnboardingStatus ResolveStatus(
        IReadOnlyCollection<OnboardingStepComputation> steps,
        bool finalizeComplete)
    {
        if (finalizeComplete)
        {
            return SubscriptionOnboardingStatus.Completed;
        }

        bool allNotStarted = steps.All(item =>
            string.Equals(item.Status, StepStatusNotStarted, StringComparison.Ordinal) ||
            string.Equals(item.Status, StepStatusBlocked, StringComparison.Ordinal));

        if (allNotStarted)
        {
            return SubscriptionOnboardingStatus.Draft;
        }

        return SubscriptionOnboardingStatus.InProgress;
    }

    private static OnboardingStateResult BuildStateResult(OnboardingComputation computed)
    {
        List<OnboardingStepState> stepStates = computed.StepStates
            .Select(item => new OnboardingStepState(
                item.Definition.Name,
                item.Status,
                item.Definition.IsSkippable,
                [.. item.Definition.DependsOn]))
            .ToList();

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

    private sealed record OnboardingStepDefinition(
        string Name,
        bool IsSkippable,
        string[] DependsOn);

    private sealed record OnboardingStepComputation(
        OnboardingStepDefinition Definition,
        string Status);

    private sealed record OnboardingComputation(
        SubscriptionOnboardingStatus Status,
        string CurrentStep,
        Guid? PrimaryAgentId,
        bool CanFinalize,
        bool FinalizeComplete,
        IReadOnlyList<OnboardingStepComputation> StepStates);
}
