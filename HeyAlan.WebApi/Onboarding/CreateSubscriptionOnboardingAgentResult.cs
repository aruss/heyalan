namespace HeyAlan.WebApi.Onboarding;

using HeyAlan.Onboarding;

public sealed record CreateSubscriptionOnboardingAgentResult(
    Guid AgentId,
    GetSubscriptionOnboardingStateResult State);
