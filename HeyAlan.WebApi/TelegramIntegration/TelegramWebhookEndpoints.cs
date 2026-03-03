namespace HeyAlan.TelegramIntegration;

using MassTransit;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeyAlan;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.Messaging;

public static class TelegramWebhookEndpoints
{
    public static IEndpointRouteBuilder MapTelegramWebhookEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        TelegramOptions options = routeBuilder.ServiceProvider.GetRequiredService<TelegramOptions>();

        routeBuilder
            .MapPost("/webhooks/telegram/{botToken}", IngestTelegramMessage)
            .WithTags("Webhooks")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter(new TelegramSecretTokenFilter(options.SecretToken));

        return routeBuilder;
    }

    private static async Task<Results<Ok, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> IngestTelegramMessage(
        [FromRoute] string botToken,
        [FromBody] IngestTelegramMessageInput input,
        IPublishEndpoint publishEndpoint,
        MainDataContext dbContext,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        ILogger logger = loggerFactory.CreateLogger("HeyAlan.TelegramWebhook");

        // Filter for text messages; silently acknowledge other update types to prevent Telegram retry loops
        if (input.Message?.Text is not { } text)
        {
            return TypedResults.Ok();
        }

        long? chatId = input.Message.Chat?.Id;
        if (!chatId.HasValue)
        {
            logger.LogWarning(
                "Rejected Telegram webhook payload without chat id for bot token prefix {BotIdPrefix}.",
                ExtractBotId(botToken));
            return TypedResults.Ok();
        }

        Agent? agent = await dbContext.Agents
            .SingleOrDefaultAsync(a => a.TelegramBotToken == botToken, ct);

        if (agent is null)
        {
            logger.LogWarning(
                "Telegram webhook bot token not found in database. Bot id prefix {BotIdPrefix}. Returning 404.",
                ExtractBotId(botToken));
            return TypedResults.NotFound();
        }

        // Telegram tokens follow the format: {BotId}:{Secret}
        string botId = botToken.Split(':')[0];
        DateTimeOffset receivedAt = input.Message.DateUnixSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(input.Message.DateUnixSeconds.Value)
            : DateTimeOffset.UtcNow;

        IncomingMessage message = new()
        {
            SubscriptionId = agent.SubscriptionId,
            AgentId = agent.Id,
            Channel = MessageChannel.Telegram,
            Role = MessageRole.Customer,
            Content = text,
            From = chatId.Value.ToString(CultureInfo.InvariantCulture),
            To = botId,
            ReceivedAt = receivedAt
        };

        await publishEndpoint.Publish(message, ct);
        logger.LogInformation(
            "Published Telegram incoming message for Subscription {SubscriptionId}, Agent {AgentId}, ChatId {ChatId}.",
            agent.SubscriptionId,
            agent.Id,
            chatId.Value);

        return TypedResults.Ok();
    }

    private static string ExtractBotId(string botToken)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            return "unknown";
        }

        int separatorIndex = botToken.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return "unknown";
        }

        return botToken[..separatorIndex];
    }
}
