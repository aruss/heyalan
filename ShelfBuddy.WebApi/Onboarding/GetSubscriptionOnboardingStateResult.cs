namespace ShelfBuddy.WebApi.Onboarding;

using ShelfBuddy.Onboarding;

public sealed record GetSubscriptionOnboardingStateResult(
    string Status,
    string CurrentStep,
    OnboardingStepState[] Steps,
    Guid? PrimaryAgentId,
    bool CanFinalize);
