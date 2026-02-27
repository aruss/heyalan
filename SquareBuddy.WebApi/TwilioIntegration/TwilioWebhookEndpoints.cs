namespace SquareBuddy.WebApi.TwilioIntegration;

using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SquareBuddy.Consumers;

public static class TwilioWebhookEndpoints
{
    public static IEndpointRouteBuilder MapTwilioWebhookEndpoints(this IEndpointRouteBuilder routeBuilder)
    {

        RouteGroupBuilder routeGroup = routeBuilder
                .MapGroup("/webhooks/twilio")
                .WithTags("Webhooks");

        routeGroup
            .MapPost("text", IngestTwilioText)
            .WithName("IngestText");

        // TODO: voice 

        return routeBuilder;
    }

    private static async Task<Results<Ok, NotFound, UnauthorizedHttpResult>> IngestTwilioText(
            [FromBody] IngestTwilioTextInput input,
            IPublishEndpoint publishEndpoint,
            CancellationToken ct)
    {
        // TODO: look for the agent by phone number, check if associated subscribtion credits left to handle it. 
        var subscribtionId = Guid.NewGuid(); // TODO: get real subscribtion id

        var message = new IncomingMessage
        {
            SubscribtionId = subscribtionId,
            Channel = MessageChannel.SMS,
            Role = MessageRole.Customer,
            Content = input.Body,
            From = input.From,
            To = input.To,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        await publishEndpoint.Publish(message, ct);

        return TypedResults.Ok();
    }
}
