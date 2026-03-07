namespace HeyAlan.Onboarding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.TelegramIntegration;

public sealed class SubscriptionOnboardingService : ISubscriptionOnboardingService
{
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

    private const string StepStatusNotStarted = "not_started";
    private const string StepStatusInProgress = "in_progress";
    private const string StepStatusCompleted = "completed";
    private const string StepStatusSkipped = "skipped";
    private const string StepStatusBlocked = "blocked";

    private readonly MainDataContext dbContext;
    private readonly ITelegramService telegramService;
    private readonly ILogger<SubscriptionOnboardingService> logger;

    static SubscriptionOnboardingService()
    {
        ValidateStepDefinitions();
    }

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
        bool resumeMode = false,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionMemberAsync(subscriptionId, userId, cancellationToken))
        {
            return new GetSubscriptionOnboardingStateResult.Failure("subscription_member_required");
        }

        OnboardingStateResult state = await this.RecomputeStateAsync(subscriptionId, cancellationToken);
        if (resumeMode)
        {
            state = ApplyResumeMode(state);
        }

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

        if (String.IsNullOrWhiteSpace(input.Name))
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

        SubscriptionOnboardingState onboardingState = await this.GetOrCreateStateAsync(agent.SubscriptionId, cancellationToken);
        onboardingState.PrimaryAgentId = agent.Id;

        string? originalTwilioPhoneNumber = agent.TwilioPhoneNumber;
        string? originalTelegramBotToken = agent.TelegramBotToken;
        string? originalWhatsappNumber = agent.WhatsappNumber;

        bool keepExistingTelegramBotToken =
            String.IsNullOrWhiteSpace(telegramBotToken) &&
            !String.IsNullOrWhiteSpace(originalTelegramBotToken);

        string? effectiveTelegramBotToken = keepExistingTelegramBotToken
            ? originalTelegramBotToken
            : telegramBotToken;

        if (String.IsNullOrWhiteSpace(twilioPhoneNumber) &&
            String.IsNullOrWhiteSpace(effectiveTelegramBotToken) &&
            String.IsNullOrWhiteSpace(whatsappNumber))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("channels_at_least_one_required");
        }

        if (!String.IsNullOrWhiteSpace(effectiveTelegramBotToken) &&
            !String.Equals(effectiveTelegramBotToken, originalTelegramBotToken, StringComparison.Ordinal))
        {
            bool isTokenUsedByAnotherAgent = await this.dbContext.Agents
                .AnyAsync(
                    item =>
                        item.Id != agent.Id &&
                        item.TelegramBotToken == effectiveTelegramBotToken,
                    cancellationToken);

            if (isTokenUsedByAnotherAgent)
            {
                return new UpdateSubscriptionOnboardingStepResult.Failure("telegram_bot_token_already_in_use");
            }
        }

        agent.TwilioPhoneNumber = twilioPhoneNumber;
        agent.TelegramBotToken = effectiveTelegramBotToken;
        agent.WhatsappNumber = whatsappNumber;

        try
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsTelegramTokenUniqueConstraintViolation(exception))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("telegram_bot_token_already_in_use");
        }

        TelegramTokenRegistrationResult registrationResult = await this.telegramService
            .RegisterWebhookIfTokenChangedAsync(
                originalTelegramBotToken,
                effectiveTelegramBotToken,
                cancellationToken);

        if (!String.IsNullOrWhiteSpace(registrationResult.ErrorCode))
        {
            this.logger.LogWarning(
                "Rolling back onboarding channel update because Telegram webhook registration failed for Subscription {SubscriptionId}, Agent {AgentId}. ErrorCode {ErrorCode}.",
                agent.SubscriptionId,
                agent.Id,
                registrationResult.ErrorCode);

            agent.TwilioPhoneNumber = originalTwilioPhoneNumber;
            agent.TelegramBotToken = originalTelegramBotToken;
            agent.WhatsappNumber = originalWhatsappNumber;

            await this.dbContext.SaveChangesAsync(cancellationToken);

            return new UpdateSubscriptionOnboardingStepResult.Failure(registrationResult.ErrorCode);
        }

        OnboardingStateResult state = 
            await this.RecomputeStateAsync(agent.SubscriptionId, cancellationToken);

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
            String.Equals(item.Definition.Name, "invitations", StringComparison.Ordinal));

        if (invitationsStep is null ||
            String.Equals(invitationsStep.Status, StepStatusBlocked, StringComparison.Ordinal))
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

        if (!computedBeforeFinalize.CanFinalize)
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

        if (String.IsNullOrWhiteSpace(step))
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
        OnboardingComputation computed = await this.ComputeStateAsync(subscriptionId, cancellationToken);

        OnboardingStepComputation? stepComputation = computed.StepStates.SingleOrDefault(item =>
            String.Equals(item.Definition.Name, definition.Name, StringComparison.Ordinal));

        if (stepComputation is not null &&
            String.Equals(stepComputation.Status, StepStatusBlocked, StringComparison.Ordinal))
        {
            return new UpdateSubscriptionOnboardingStepResult.Failure("step_skip_blocked");
        }

        int currentStepIndex = GetStepIndex(onboardingState.CurrentStep);
        int targetStepIndex = GetStepIndex(definition.Name);

        if (targetStepIndex >= currentStepIndex)
        {
            string nextStep = ResolveNextStepAfterIndex(targetStepIndex, computed.StepStates);
            onboardingState.CurrentStep = nextStep;
        }

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
        OnboardingProfilePrefill profilePrefill = new(
            primaryAgent?.Name,
            primaryAgent?.Personality);
        OnboardingChannelsPrefill channelsPrefill = new(
            primaryAgent?.TwilioPhoneNumber,
            primaryAgent?.WhatsappNumber,
            !String.IsNullOrWhiteSpace(primaryAgent?.TelegramBotToken));

        Dictionary<string, bool> completedByStep = new(StringComparer.Ordinal)
        {
            ["square_connect"] = IsSquareConnectComplete(connection),
            ["profile"] = IsProfileComplete(primaryAgent),
            ["channels"] = IsChannelsComplete(primaryAgent),
            ["invitations"] =
                onboardingState.Status == SubscriptionOnboardingStatus.Completed ||
                String.Equals(onboardingState.CurrentStep, "finalize", StringComparison.Ordinal),
            ["finalize"] = onboardingState.Status == SubscriptionOnboardingStatus.Completed && onboardingState.CompletedAt.HasValue
        };

        int configuredCurrentIndex = GetStepIndex(onboardingState.CurrentStep);

        List<OnboardingStepComputation> baseStepStates = [];
        for (int index = 0; index < StepDefinitions.Length; index++)
        {
            OnboardingStepDefinition definition = StepDefinitions[index];
            bool isCompleted = completedByStep.TryGetValue(definition.Name, out bool completion) && completion;

            if (isCompleted)
            {
                baseStepStates.Add(new OnboardingStepComputation(definition, StepStatusCompleted));
                continue;
            }

            bool hasSkippedDependency = definition.DependsOn.Any(dependency =>
            {
                OnboardingStepComputation? dependencyState = baseStepStates.SingleOrDefault(item =>
                    String.Equals(item.Definition.Name, dependency, StringComparison.Ordinal));

                return dependencyState is not null &&
                    String.Equals(dependencyState.Status, StepStatusSkipped, StringComparison.Ordinal);
            });

            if (hasSkippedDependency)
            {
                baseStepStates.Add(new OnboardingStepComputation(definition, StepStatusSkipped));
                continue;
            }

            bool hasUnmetDependency = definition.DependsOn.Any(dependency =>
            {
                OnboardingStepComputation? dependencyState = baseStepStates.SingleOrDefault(item =>
                    String.Equals(item.Definition.Name, dependency, StringComparison.Ordinal));

                return dependencyState is null ||
                    !String.Equals(dependencyState.Status, StepStatusCompleted, StringComparison.Ordinal);
            });

            if (hasUnmetDependency)
            {
                baseStepStates.Add(new OnboardingStepComputation(definition, StepStatusBlocked));
                continue;
            }

            bool shouldMarkSkipped = definition.IsSkippable &&
                (configuredCurrentIndex > index || HasCompletedLaterStep(index, completedByStep));

            baseStepStates.Add(new OnboardingStepComputation(
                definition,
                shouldMarkSkipped ? StepStatusSkipped : StepStatusNotStarted));
        }

        string currentStep = ResolveCurrentStep(onboardingState.CurrentStep, baseStepStates);

        List<OnboardingStepComputation> normalizedStepStates = [];
        foreach (OnboardingStepComputation stepState in baseStepStates)
        {
            if (String.Equals(stepState.Definition.Name, currentStep, StringComparison.Ordinal) &&
                String.Equals(stepState.Status, StepStatusNotStarted, StringComparison.Ordinal))
            {
                normalizedStepStates.Add(new OnboardingStepComputation(stepState.Definition, StepStatusInProgress));
                continue;
            }

            normalizedStepStates.Add(stepState);
        }

        bool hasIncompleteRequiredSteps = normalizedStepStates.Any(item =>
            (String.Equals(item.Definition.Name, "square_connect", StringComparison.Ordinal) || !item.Definition.IsSkippable) &&
            !String.Equals(item.Definition.Name, "finalize", StringComparison.Ordinal) &&
            !String.Equals(item.Status, StepStatusCompleted, StringComparison.Ordinal));

        bool canFinalize = !hasIncompleteRequiredSteps;
        bool finalizeComplete = completedByStep["finalize"];
        SubscriptionOnboardingStatus status = ResolveStatus(normalizedStepStates, finalizeComplete);

        return new OnboardingComputation(
            status,
            finalizeComplete ? "finalize" : currentStep,
            primaryAgentId,
            canFinalize,
            normalizedStepStates,
            profilePrefill,
            channelsPrefill);
    }

    private static bool HasCompletedLaterStep(int stepIndex, IReadOnlyDictionary<string, bool> completedByStep)
    {
        for (int index = stepIndex + 1; index < StepDefinitions.Length; index++)
        {
            string stepName = StepDefinitions[index].Name;
            if (completedByStep.TryGetValue(stepName, out bool isCompleted) && isCompleted)
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveCurrentStep(string configuredCurrentStep, IReadOnlyCollection<OnboardingStepComputation> baseStepStates)
    {
        OnboardingStepComputation? configured = baseStepStates.SingleOrDefault(item =>
            String.Equals(item.Definition.Name, configuredCurrentStep, StringComparison.Ordinal));

        if (configured is not null &&
            (String.Equals(configured.Status, StepStatusNotStarted, StringComparison.Ordinal) ||
             String.Equals(configured.Status, StepStatusInProgress, StringComparison.Ordinal)))
        {
            return configured.Definition.Name;
        }

        OnboardingStepComputation? nextNotStarted = baseStepStates.FirstOrDefault(item =>
            String.Equals(item.Status, StepStatusNotStarted, StringComparison.Ordinal));

        return nextNotStarted?.Definition.Name ?? "finalize";
    }

    private static int GetStepIndex(string step)
    {
        int index = Array.FindIndex(
            StepDefinitions,
            item => String.Equals(item.Name, step, StringComparison.Ordinal));

        return index < 0 ? 0 : index;
    }

    private static string ResolveNextStepAfterIndex(int index, IReadOnlyList<OnboardingStepComputation> states)
    {
        for (int currentIndex = index + 1; currentIndex < states.Count; currentIndex++)
        {
            string status = states[currentIndex].Status;
            if (String.Equals(status, StepStatusNotStarted, StringComparison.Ordinal) ||
                String.Equals(status, StepStatusInProgress, StringComparison.Ordinal))
            {
                return states[currentIndex].Definition.Name;
            }
        }

        return "finalize";
    }

    private static bool IsSquareConnectComplete(SubscriptionSquareConnection? connection)
    {
        if (connection is null)
        {
            return false;
        }

        if (String.IsNullOrWhiteSpace(connection.EncryptedAccessToken) ||
            String.IsNullOrWhiteSpace(connection.EncryptedRefreshToken))
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
            !String.IsNullOrWhiteSpace(agent.Name) &&
            agent.Personality.HasValue;
    }

    private static bool IsChannelsComplete(Agent? agent)
    {
        return agent is not null &&
            (
                !String.IsNullOrWhiteSpace(agent.TwilioPhoneNumber) ||
                !String.IsNullOrWhiteSpace(agent.TelegramBotToken) ||
                !String.IsNullOrWhiteSpace(agent.WhatsappNumber)
            );
    }

    private static string? NormalizeOptionalChannel(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static OnboardingStepDefinition? GetStepDefinition(string step)
    {
        return StepDefinitions.SingleOrDefault(item =>
            String.Equals(item.Name, step, StringComparison.Ordinal));
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

        bool hasProgress = steps.Any(item =>
            String.Equals(item.Status, StepStatusCompleted, StringComparison.Ordinal) ||
            String.Equals(item.Status, StepStatusSkipped, StringComparison.Ordinal) ||
            String.Equals(item.Status, StepStatusInProgress, StringComparison.Ordinal));

        return hasProgress
            ? SubscriptionOnboardingStatus.InProgress
            : SubscriptionOnboardingStatus.Draft;
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
            computed.CanFinalize,
            computed.ProfilePrefill,
            computed.ChannelsPrefill);
    }

    private static OnboardingStateResult ApplyResumeMode(OnboardingStateResult state)
    {
        if (String.Equals(state.Status, SubscriptionOnboardingStatus.Completed.ToString(), StringComparison.Ordinal))
        {
            return state;
        }

        bool squareCompleted = state.Steps.Any(item =>
            String.Equals(item.Step, "square_connect", StringComparison.Ordinal) &&
            String.Equals(item.Status, StepStatusCompleted, StringComparison.Ordinal));

        if (squareCompleted)
        {
            return state;
        }

        OnboardingStepState[] steps = state.Steps
            .Select(item =>
            {
                if (!String.Equals(item.Step, "square_connect", StringComparison.Ordinal))
                {
                    return item;
                }

                return new OnboardingStepState(
                    item.Step,
                    StepStatusInProgress,
                    item.IsSkippable,
                    item.DependsOn);
            })
            .ToArray();

        return state with
        {
            CurrentStep = "square_connect",
            Steps = steps,
            CanFinalize = false
        };
    }

    private static bool IsTelegramTokenUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return false;
        }

        return String.Equals(postgresException.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal) &&
            !String.IsNullOrWhiteSpace(postgresException.ConstraintName) &&
            postgresException.ConstraintName.Contains("telegram_bot_token", StringComparison.Ordinal);
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
        IReadOnlyList<OnboardingStepComputation> StepStates,
        OnboardingProfilePrefill ProfilePrefill,
        OnboardingChannelsPrefill ChannelsPrefill);
}
