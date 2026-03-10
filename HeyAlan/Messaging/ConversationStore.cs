namespace HeyAlan.Messaging;

using Microsoft.EntityFrameworkCore;
using HeyAlan.Data;
using HeyAlan.Data.Entities;

public class ConversationStore : IConversationStore
{
    private const int LastMessagePreviewLimit = 160;
    private readonly MainDataContext dbContext;

    public ConversationStore(MainDataContext dbContext)
    {
        this.dbContext = dbContext ?? 
            throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task UpsertIncomingMessageAsync(
        IncomingMessage message,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        Conversation conversation = await this.GetOrCreateConversationAsync(
            message.AgentId,
            message.From,
            message.Channel,
            ct);

        ConversationMessage conversationMessage = new()
        {
            ConversationId = conversation.Id,
            AgentId = message.AgentId,
            Role = message.Role,
            Content = message.Content,
            From = message.From,
            To = message.To,
            OccurredAt = message.ReceivedAt,
            IsRead = message.Role != MessageRole.Customer,
            ReadAt = message.Role == MessageRole.Customer ? null : message.ReceivedAt
        };

        this.dbContext.ConversationMessages.Add(conversationMessage);

        this.ApplySummaryFromMessage(conversation, conversationMessage);

        if (conversationMessage.Role == MessageRole.Customer)
        {
            conversation.UnreadCount += 1;
        }

        await this.dbContext.SaveChangesAsync(ct);
    }

    public async Task AppendOutgoingTelegramMessageAsync(
        OutgoingTelegramMessage message,
        DateTimeOffset occurredAt,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        Conversation conversation = await this.GetOrCreateConversationAsync(
            message.AgentId,
            message.To,
            MessageChannel.Telegram,
            ct);

        string from = await this.ResolveTelegramBotIdAsync(message.AgentId, ct);

        ConversationMessage conversationMessage = new()
        {
            ConversationId = conversation.Id,
            AgentId = message.AgentId,
            Role = MessageRole.Agent,
            Content = message.Content,
            From = from,
            To = message.To,
            OccurredAt = occurredAt,
            IsRead = true,
            ReadAt = occurredAt
        };

        this.dbContext.ConversationMessages.Add(conversationMessage);
        this.ApplySummaryFromMessage(conversation, conversationMessage);

        await this.dbContext.SaveChangesAsync(ct);
    }

    private async Task<Conversation> GetOrCreateConversationAsync(
        Guid agentId,
        string participantExternalId,
        MessageChannel channel,
        CancellationToken ct)
    {
        Conversation? existingConversation = await this.dbContext.Conversations
            .SingleOrDefaultAsync(
                conversation =>
                    conversation.AgentId == agentId &&
                    conversation.ParticipantExternalId == participantExternalId &&
                    conversation.Channel == channel,
                ct);

        if (existingConversation is not null)
        {
            return existingConversation;
        }

        Conversation createdConversation = new()
        {
            AgentId = agentId,
            ParticipantExternalId = participantExternalId,
            Channel = channel,
            UnreadCount = 0
        };

        this.dbContext.Conversations.Add(createdConversation);

        try
        {
            await this.dbContext.SaveChangesAsync(ct);
            return createdConversation;
        }
        catch (DbUpdateException)
        {
            this.dbContext.Entry(createdConversation).State = EntityState.Detached;
        }

        Conversation conversation = await this.dbContext.Conversations
            .SingleAsync(
                item =>
                    item.AgentId == agentId &&
                    item.ParticipantExternalId == participantExternalId &&
                    item.Channel == channel,
                ct);

        return conversation;
    }

    private async Task<string> ResolveTelegramBotIdAsync(
        Guid agentId,
        CancellationToken ct)
    {
        string? token = await this.dbContext.Agents
            .Where(agent => agent.Id == agentId)
            .Select(agent => agent.TelegramBotToken)
            .SingleOrDefaultAsync(ct);

        if (String.IsNullOrWhiteSpace(token))
        {
            return String.Empty;
        }

        int separatorIndex = token.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return String.Empty;
        }

        return token[..separatorIndex];
    }

    private void ApplySummaryFromMessage(
        Conversation conversation,
        ConversationMessage message)
    {
        conversation.LastMessageAt = message.OccurredAt;
        conversation.LastMessageRole = message.Role;
        conversation.LastMessagePreview = this.BuildPreview(message.Content);
    }

    private string BuildPreview(string content)
    {
        if (String.IsNullOrWhiteSpace(content))
        {
            return String.Empty;
        }

        if (content.Length <= LastMessagePreviewLimit)
        {
            return content;
        }

        return content[..LastMessagePreviewLimit];
    }
}
