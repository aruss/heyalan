namespace ShelfBuddy.WebApi.Onboarding;

public sealed record PatchOnboardingAgentChannelsInput(
    string? TwilioPhoneNumber,
    string? TelegramBotToken,
    string? WhatsappNumber);
