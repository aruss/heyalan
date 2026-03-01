namespace ShelfBuddy.WebApi.Onboarding;

using ShelfBuddy.Onboarding;

public sealed record CreateSubscriptionOnboardingAgentResult(
    Guid AgentId,
    GetSubscriptionOnboardingStateResult State);
