namespace HeyAlan.Onboarding;

using HeyAlan.Data.Entities;

public sealed record OnboardingStepState(
    string Step,
    string Status,
    bool IsSkippable,
    string[] DependsOn);

public sealed record OnboardingProfilePrefill(
    string? Name,
    AgentPersonality? Personality);

public sealed record OnboardingChannelsPrefill(
    string? TwilioPhoneNumber,
    string? WhatsappNumber,
    bool HasTelegramBotToken);

public sealed record OnboardingStateResult(
    string Status,
    string CurrentStep,
    OnboardingStepState[] Steps,
    Guid? PrimaryAgentId,
    bool CanFinalize,
    OnboardingProfilePrefill ProfilePrefill,
    OnboardingChannelsPrefill ChannelsPrefill);

public abstract record GetSubscriptionOnboardingStateResult
{
    public sealed record Success(OnboardingStateResult State) : GetSubscriptionOnboardingStateResult;

    public sealed record Failure(string ErrorCode) : GetSubscriptionOnboardingStateResult;
}

public abstract record CreateSubscriptionOnboardingAgentResult
{
    public sealed record Success(Guid AgentId, OnboardingStateResult State) : CreateSubscriptionOnboardingAgentResult;

    public sealed record Failure(string ErrorCode) : CreateSubscriptionOnboardingAgentResult;
}

public abstract record UpdateSubscriptionOnboardingStepResult
{
    public sealed record Success(OnboardingStateResult State) : UpdateSubscriptionOnboardingStepResult;

    public sealed record Failure(string ErrorCode) : UpdateSubscriptionOnboardingStepResult;
}

public sealed record UpdateSubscriptionOnboardingProfileInput(
    Guid AgentId,
    Guid UserId,
    string? Name,
    AgentPersonality? Personality);

public sealed record UpdateSubscriptionOnboardingChannelsInput(
    Guid AgentId,
    Guid UserId,
    string? TwilioPhoneNumber,
    string? TelegramBotToken,
    string? WhatsappNumber);

public interface ISubscriptionOnboardingService
{
    Task<GetSubscriptionOnboardingStateResult> GetStateAsync(
        Guid subscriptionId,
        Guid userId,
        bool resumeMode = false,
        CancellationToken cancellationToken = default);

    Task<CreateSubscriptionOnboardingAgentResult> CreatePrimaryAgentAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UpdateSubscriptionOnboardingStepResult> UpdateProfileAsync(
        UpdateSubscriptionOnboardingProfileInput input,
        CancellationToken cancellationToken = default);

    Task<UpdateSubscriptionOnboardingStepResult> UpdateChannelsAsync(
        UpdateSubscriptionOnboardingChannelsInput input,
        CancellationToken cancellationToken = default);

    Task<UpdateSubscriptionOnboardingStepResult> CompleteInvitationsAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UpdateSubscriptionOnboardingStepResult> FinalizeAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UpdateSubscriptionOnboardingStepResult> SkipStepAsync(
        Guid subscriptionId,
        Guid userId,
        string step,
        CancellationToken cancellationToken = default);

    Task<OnboardingStateResult> RecomputeStateAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}
