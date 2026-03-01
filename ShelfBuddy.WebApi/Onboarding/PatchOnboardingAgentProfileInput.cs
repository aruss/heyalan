namespace ShelfBuddy.WebApi.Onboarding;

using ShelfBuddy.Data.Entities;

public sealed record PatchOnboardingAgentProfileInput(
    string? Name,
    AgentPersonality? Personality);
