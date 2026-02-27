namespace ShelfBuddy;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter<MessageChannel>))]
public enum MessageChannel
{
    WhatsApp,
    Telegram,
    SMS
}

[JsonConverter(typeof(JsonStringEnumConverter<MessageRole>))]
public enum MessageRole
{
    Customer,
    Agent,
    Operator,
}