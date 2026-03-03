namespace HeyAlan.Data.Entities;

/// <summary>
/// Has a list of Users and HeyAlans associated with it and membership tier info syched from the payment provider
/// </summary>
public class Agent : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public AgentPersonality? Personality { get; set; }

    public string? PersonalityPromptRaw { get; set; }

    public string? PersonalityPromptSanitized { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // twilio
    public string? TwilioPhoneNumber { get; set; } = null!;

    // telegram 
    public string? TelegramBotToken { get; set; } = null!;

    // whatsapp
    public string? WhatsappNumber { get; set; } = null!;
}
