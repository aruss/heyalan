namespace HeyAlan.TelegramIntegration;

using System.Text.Json.Serialization;

public sealed class IngestTelegramMessageInput
{
    [JsonPropertyName("message")]
    public TelegramMessageInput? Message { get; set; }
}

public sealed class TelegramMessageInput
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("date")]
    public long? DateUnixSeconds { get; set; }

    [JsonPropertyName("from")]
    public TelegramUserInput? From { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChatInput? Chat { get; set; }
}

public sealed class TelegramUserInput
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}

public sealed class TelegramChatInput
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }
}
