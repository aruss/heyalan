namespace HeyAlan.Messaging;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.TelegramIntegration;

public class OutgoingTelegramMessageConsumer
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

    public async Task Consume(OutgoingTelegramMessage message, CancellationToken ct)
    {
        this.logger.LogInformation(
            "Sending Telegram outbound message for Subscription {SubscriptionId}, Agent {AgentId}",
            message.SubscriptionId,
            message.AgentId);

        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(a => a.Id == message.AgentId, ct);

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
            ct);

        await this.conversationStore.AppendOutgoingTelegramMessageAsync(
            message,
            DateTimeOffset.UtcNow,
            ct);
    }
}
