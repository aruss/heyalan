namespace ShelfBuddy.TelegramIntegration;

using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShelfBuddy;
using ShelfBuddy.Consumers;
using ShelfBuddy.Data;
using ShelfBuddy.Data.Entities;

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
            CancellationToken ct)
    {
        // Filter for text messages; silently acknowledge other update types to prevent Telegram retry loops
        if (input.Message?.Text is not { } text)
        {
            return TypedResults.Ok();
        }

        Agent? agent = await dbContext.Agents
            .SingleOrDefaultAsync(a => a.TelegramBotToken == botToken, ct);

        if (agent is null)
        {
            return TypedResults.NotFound();
        }

        // Telegram tokens follow the format: {BotId}:{Secret}
        string botId = botToken.Split(':')[0];
        DateTimeOffset receivedAt = input.Message.DateUnixSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(input.Message.DateUnixSeconds.Value)
            : DateTimeOffset.UtcNow;

        IncomingMessage message = new()
        {
            SubscribtionId = agent.SubscriptionId,
            AgentId = agent.Id,
            Channel = MessageChannel.Telegram,
            Role = MessageRole.Customer,
            Content = text,
            From = input.Message.From?.Id.ToString() ?? string.Empty,
            To = botId,
            ReceivedAt = receivedAt
        };

        await publishEndpoint.Publish(message, ct);

        return TypedResults.Ok();
    }
}
