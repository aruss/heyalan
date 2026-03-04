namespace HeyAlan.WebApi.TwilioIntegration;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using HeyAlan.Messaging;

public static class TwilioWebhookEndpoints
{
    public static IEndpointRouteBuilder MapTwilioWebhookEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        RouteGroupBuilder routeGroup = routeBuilder
            .MapGroup("/webhooks/twilio")
            .WithTags("Webhooks");

        routeGroup
            .MapPost("text", IngestTwilioText)
            .WithName("IngestText")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // TODO: voice

        return routeBuilder;
    }

    private static async Task<Results<Ok, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> IngestTwilioText(
        [FromBody] IngestTwilioTextInput input,
        IMessageBus messageBus,
        CancellationToken ct)
    {
        // TODO: look for the agent by phone number, check if associated subscription credits left to handle it.
        Guid subscriptionId = Guid.NewGuid();
        Guid agentId = Guid.NewGuid();

        IncomingMessage message = new()
        {
            SubscriptionId = subscriptionId,
            AgentId = agentId,
            Channel = MessageChannel.SMS,
            Role = MessageRole.Customer,
            Content = input.Body,
            From = input.From,
            To = input.To,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        await messageBus.PublishAsync(message);

        return TypedResults.Ok();
    }
}
