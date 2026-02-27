namespace SquareBuddy.TelegramIntegration;

using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SquareBuddy;
using SquareBuddy.Consumers;

public static class TelegramWebhookEndpoints
{
    public static IEndpointRouteBuilder MapTelegramWebhookEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        TelegramOptions options = routeBuilder.ServiceProvider.GetRequiredService<TelegramOptions>(); 

        routeBuilder
            .MapPost("/webhooks/telegram/{botToken}", IngestTelegramMessage)
            .WithTags("Telegram")
            .AddEndpointFilter(new TelegramSecretTokenFilter(options.SecretToken));

        return routeBuilder;
    }
     
    private static async Task<Results<Ok, NotFound, UnauthorizedHttpResult>> IngestTelegramMessage(
            [FromRoute] string botToken,
            [FromBody] Telegram.Bot.Types.Update input, 
            IPublishEndpoint publishEndpoint,
            CancellationToken ct)
    {
        // Filter for text messages; silently acknowledge other update types to prevent Telegram retry loops
        if (input.Message?.Text is not { } text)
        {
            return TypedResults.Ok();
        }

        // Telegram tokens follow the format: {BotId}:{Secret}
        string botId = botToken.Split(':')[0];

        var message = new IncomingMessage
        {
            SubscribtionId = default, // Assign resolved Guid based on botToken lookup
            Channel = MessageChannel.Telegram, // Assuming addition to your enum
            Role = MessageRole.Customer,
            Content = text,
            From = input.Message.From?.Id.ToString() ?? string.Empty,
            To = botId,
            ReceivedAt = input.Message.Date
        };

        await publishEndpoint.Publish(message, ct);

        return TypedResults.Ok();
    }
}
