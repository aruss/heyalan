namespace HeyAlan.WebApi.Onboarding;

using HeyAlan.Onboarding;

public sealed record GetSubscriptionOnboardingStateResult(
    string Status,
    string CurrentStep,
    OnboardingStepState[] Steps,
    Guid? PrimaryAgentId,
    bool CanFinalize,
    OnboardingProfilePrefill ProfilePrefill,
    OnboardingChannelsPrefill ChannelsPrefill);
