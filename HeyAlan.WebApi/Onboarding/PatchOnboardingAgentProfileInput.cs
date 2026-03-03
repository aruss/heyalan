namespace HeyAlan.WebApi.Onboarding;

using HeyAlan.Data.Entities;

public sealed record PatchOnboardingAgentProfileInput(
    string? Name,
    AgentPersonality? Personality);
