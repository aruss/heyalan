namespace SquareBuddy.Consumers;

using MassTransit;
using Microsoft.Extensions.Logging;

public record IncomingMessage
{
    public Guid SubscribtionId { get; init; }

    public MessageChannel Channel { get; init; }

    public MessageRole Role { get; set; }

    public string Content { get; init; } = string.Empty;

    public string From { get; init; } = string.Empty;

    public string To { get; init; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; init; }
}

public class IncomingMessageConsumer : IConsumer<IncomingMessage>
{
    private readonly ILogger<IncomingMessageConsumer> logger;

    public IncomingMessageConsumer(
        ILogger<IncomingMessageConsumer> logger)
    {
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<IncomingMessage> context)
    {
        var message = context.Message;

        this.logger.LogInformation(
            "Subscribtion {SubscribtionId} received {Channel} message from {From}",
            message.SubscribtionId, message.Channel, message.From);

        // 2. Do your database work safely
        // ...
    }
}