namespace ShelfBuddy.Tests;

using ShelfBuddy.TelegramIntegration;
using System.Text.Json;

public class TelegramWebhookInputTests
{
    [Fact]
    public void IngestTelegramMessageInput_DeserializesChatId()
    {
        const string Payload = """
            {
              "message": {
                "text": "hello",
                "date": 1734567890,
                "chat": {
                  "id": 9988776655
                },
                "from": {
                  "id": 123
                }
              }
            }
            """;

        IngestTelegramMessageInput? input = JsonSerializer.Deserialize<IngestTelegramMessageInput>(Payload);

        Assert.NotNull(input);
        Assert.NotNull(input!.Message);
        Assert.Equal(9988776655L, input.Message!.Chat?.Id);
    }
}
