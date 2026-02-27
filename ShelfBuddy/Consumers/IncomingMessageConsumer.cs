namespace ShelfBuddy.Consumers;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelfBuddy.Core.Conversations;
using ShelfBuddy.Data;
using ShelfBuddy.Data.Entities;
using ShelfBuddy.TelegramIntegration;

public record IncomingMessage
{
    public Guid SubscribtionId { get; init; }

    public Guid AgentId { get; init; }

    public MessageChannel Channel { get; init; }

    public MessageRole Role { get; set; }

    public string Content { get; init; } = string.Empty;

    public string From { get; init; } = string.Empty;

    public string To { get; init; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; init; }
}

public class IncomingMessageConsumer : IConsumer<IncomingMessage>
{
    private const string PlaceholderReply = "Thanks, we got your message.";

    private readonly ILogger<IncomingMessageConsumer> logger;
    private readonly IPublishEndpoint publishEndpoint;
    private readonly IConversationStore conversationStore;

    public IncomingMessageConsumer(
        ILogger<IncomingMessageConsumer> logger,
        IPublishEndpoint publishEndpoint,
        IConversationStore conversationStore)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        this.conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
    }

    public async Task Consume(ConsumeContext<IncomingMessage> context)
    {
        IncomingMessage message = context.Message;

        this.logger.LogInformation(
            "Subscribtion {SubscribtionId} Agent {AgentId} received {Channel} message from {From}",
            message.SubscribtionId,
            message.AgentId,
            message.Channel,
            message.From);

        await this.conversationStore.UpsertIncomingMessageAsync(message, context.CancellationToken);

        // Processes the message here ...
        if (message.Channel == MessageChannel.Telegram)
        {
            OutgoingTelegramMessage telegramMessage = new()
            {
                SubscribtionId = message.SubscribtionId,
                AgentId = message.AgentId,
                Content = PlaceholderReply,
                To = message.From
            };

            await this.publishEndpoint.Publish(telegramMessage, context.CancellationToken);
        }
    }
}

public record OutgoingTelegramMessage
{
    public Guid SubscribtionId { get; init; }

    public Guid AgentId { get; init; }

    public string Content { get; init; } = string.Empty;

    public string To { get; init; } = string.Empty;
}

public class OutgoingTelegramMessageConsumer : IConsumer<OutgoingTelegramMessage>
{
    private readonly ILogger<OutgoingTelegramMessageConsumer> logger;
    private readonly MainDataContext dbContext;
    private readonly ITelegramService telegramService;
    private readonly IConversationStore conversationStore;

    public OutgoingTelegramMessageConsumer(
        ILogger<OutgoingTelegramMessageConsumer> logger,
        ITelegramService telegramService,
        MainDataContext dbContext,
        IConversationStore conversationStore)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.telegramService = telegramService ?? throw new ArgumentNullException(nameof(telegramService));
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
    }

    public async Task Consume(ConsumeContext<OutgoingTelegramMessage> context)
    {
        OutgoingTelegramMessage message = context.Message;

        this.logger.LogInformation(
            "Sending Telegram outbound message for Subscribtion {SubscribtionId}, Agent {AgentId}",
            message.SubscribtionId,
            message.AgentId);

        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(a => a.Id == message.AgentId, context.CancellationToken);

        if (agent is null)
        {
            throw new InvalidOperationException(
                $"Agent '{message.AgentId}' was not found for outgoing Telegram message.");
        }

        if (string.IsNullOrWhiteSpace(agent.TelegramBotToken))
        {
            throw new InvalidOperationException(
                $"Telegram bot token is not configured for agent '{message.AgentId}'.");
        }

        if (!long.TryParse(message.To, out long chatId))
        {
            throw new FormatException(
                $"Outgoing Telegram recipient '{message.To}' is not a valid numeric chat id.");
        }

        await this.telegramService.SendMessageAsync(
            agent.TelegramBotToken,
            chatId,
            message.Content,
            context.CancellationToken);

        await this.conversationStore.AppendOutgoingTelegramMessageAsync(
            message,
            DateTimeOffset.UtcNow,
            context.CancellationToken);
    }
}
