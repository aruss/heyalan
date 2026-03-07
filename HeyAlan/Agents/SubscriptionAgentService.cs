namespace HeyAlan.Agents;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.TelegramIntegration;

public sealed class SubscriptionAgentService : ISubscriptionAgentService
{
    private readonly MainDataContext dbContext;
    private readonly ITelegramService telegramService;
    private readonly ILogger<SubscriptionAgentService> logger;

    public SubscriptionAgentService(
        MainDataContext dbContext,
        ITelegramService telegramService,
        ILogger<SubscriptionAgentService> logger)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.telegramService = telegramService ?? throw new ArgumentNullException(nameof(telegramService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetSubscriptionAgentsResult> GetAgentsAsync(
        GetSubscriptionAgentsInput input,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionMemberAsync(input.SubscriptionId, input.UserId, cancellationToken))
        {
            return new GetSubscriptionAgentsResult.Failure("subscription_member_required");
        }

        List<SubscriptionAgentListItem> list = await this.dbContext.Agents
            .Where(item => item.SubscriptionId == input.SubscriptionId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Select(item => new SubscriptionAgentListItem(
                item.Id,
                item.Name,
                item.Personality,
                IsOperationalReady(item),
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new GetSubscriptionAgentsResult.Success(list);
    }

    public async Task<CreateSubscriptionAgentResult> CreateAgentAsync(
        CreateSubscriptionAgentInput input,
        CancellationToken cancellationToken = default)
    {
        if (!await this.IsSubscriptionMemberAsync(input.SubscriptionId, input.UserId, cancellationToken))
        {
            return new CreateSubscriptionAgentResult.Failure("subscription_member_required");
        }

        Agent agent = new()
        {
            SubscriptionId = input.SubscriptionId,
            Name = "Draft Agent"
        };

        this.dbContext.Agents.Add(agent);
        await this.dbContext.SaveChangesAsync(cancellationToken);

        return new CreateSubscriptionAgentResult.Success(ToAgentDetailsResult(agent));
    }

    public async Task<GetAgentResult> GetAgentAsync(
        GetAgentInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new GetAgentResult.Failure("agent_not_found");
        }

        if (!await this.IsSubscriptionMemberAsync(agent.SubscriptionId, input.UserId, cancellationToken))
        {
            return new GetAgentResult.Failure("subscription_member_required");
        }

        return new GetAgentResult.Success(ToAgentDetailsResult(agent));
    }

    public async Task<UpdateAgentResult> UpdateAgentAsync(
        UpdateAgentInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new UpdateAgentResult.Failure("agent_not_found");
        }

        if (!await this.IsSubscriptionMemberAsync(agent.SubscriptionId, input.UserId, cancellationToken))
        {
            return new UpdateAgentResult.Failure("subscription_member_required");
        }

        if (String.IsNullOrWhiteSpace(input.Name))
        {
            return new UpdateAgentResult.Failure("agent_name_required");
        }

        if (!input.Personality.HasValue)
        {
            return new UpdateAgentResult.Failure("agent_personality_required");
        }

        string? normalizedTwilioPhoneNumber = NormalizeOptionalValue(input.TwilioPhoneNumber);
        string? normalizedWhatsappNumber = NormalizeOptionalValue(input.WhatsappNumber);
        string? normalizedTelegramBotToken = NormalizeOptionalValue(input.TelegramBotToken);
        string? normalizedPersonalityPromptRaw = NormalizeOptionalValue(input.PersonalityPromptRaw);
        string normalizedName = input.Name.Trim();

        string? originalTwilioPhoneNumber = agent.TwilioPhoneNumber;
        string? originalWhatsappNumber = agent.WhatsappNumber;
        string? originalTelegramBotToken = agent.TelegramBotToken;

        if (!String.IsNullOrWhiteSpace(normalizedTelegramBotToken) &&
            !String.Equals(normalizedTelegramBotToken, originalTelegramBotToken, StringComparison.Ordinal))
        {
            bool tokenInUse = await this.dbContext.Agents
                .AnyAsync(
                    item =>
                        item.Id != agent.Id &&
                        item.TelegramBotToken == normalizedTelegramBotToken,
                    cancellationToken);

            if (tokenInUse)
            {
                return new UpdateAgentResult.Failure("telegram_bot_token_already_in_use");
            }
        }

        agent.Name = normalizedName;
        agent.Personality = input.Personality.Value;
        agent.PersonalityPromptRaw = normalizedPersonalityPromptRaw;
        agent.TwilioPhoneNumber = normalizedTwilioPhoneNumber;
        agent.WhatsappNumber = normalizedWhatsappNumber;
        agent.TelegramBotToken = normalizedTelegramBotToken;

        try
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsTelegramTokenUniqueConstraintViolation(exception))
        {
            return new UpdateAgentResult.Failure("telegram_bot_token_already_in_use");
        }

        TelegramTokenRegistrationResult registrationResult = await this.telegramService
            .RegisterWebhookIfTokenChangedAsync(
                originalTelegramBotToken,
                normalizedTelegramBotToken,
                cancellationToken);

        if (!String.IsNullOrWhiteSpace(registrationResult.ErrorCode))
        {
            this.logger.LogWarning(
                "Rolling back agent settings channel update because Telegram webhook registration failed for Subscription {SubscriptionId}, Agent {AgentId}. ErrorCode {ErrorCode}.",
                agent.SubscriptionId,
                agent.Id,
                registrationResult.ErrorCode);

            agent.TwilioPhoneNumber = originalTwilioPhoneNumber;
            agent.WhatsappNumber = originalWhatsappNumber;
            agent.TelegramBotToken = originalTelegramBotToken;

            await this.dbContext.SaveChangesAsync(cancellationToken);

            return new UpdateAgentResult.Failure(registrationResult.ErrorCode);
        }

        return new UpdateAgentResult.Success(ToAgentDetailsResult(agent));
    }

    public async Task<DeleteAgentResult> DeleteAgentAsync(
        DeleteAgentInput input,
        CancellationToken cancellationToken = default)
    {
        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == input.AgentId, cancellationToken);

        if (agent is null)
        {
            return new DeleteAgentResult.Failure("agent_not_found");
        }

        if (!await this.IsSubscriptionMemberAsync(agent.SubscriptionId, input.UserId, cancellationToken))
        {
            return new DeleteAgentResult.Failure("subscription_member_required");
        }

        this.dbContext.Agents.Remove(agent);
        await this.dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteAgentResult.Success();
    }

    private async Task<bool> IsSubscriptionMemberAsync(
        Guid subscriptionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (subscriptionId == Guid.Empty || userId == Guid.Empty)
        {
            return false;
        }

        bool isMember = await this.dbContext.SubscriptionUsers
            .AnyAsync(
                membership =>
                    membership.SubscriptionId == subscriptionId &&
                    membership.UserId == userId,
                cancellationToken);

        return isMember;
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static bool IsTelegramTokenUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return false;
        }

        return String.Equals(postgresException.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal) &&
            !String.IsNullOrWhiteSpace(postgresException.ConstraintName) &&
            postgresException.ConstraintName.Contains("telegram_bot_token", StringComparison.Ordinal);
    }

    private static AgentDetailsResult ToAgentDetailsResult(Agent agent)
    {
        return new AgentDetailsResult(
            agent.Id,
            agent.SubscriptionId,
            agent.Name,
            agent.Personality,
            agent.PersonalityPromptRaw,
            agent.PersonalityPromptSanitized,
            agent.TwilioPhoneNumber,
            agent.WhatsappNumber,
            agent.TelegramBotToken,
            IsOperationalReady(agent),
            agent.CreatedAt,
            agent.UpdatedAt);
    }

    private static bool IsOperationalReady(Agent agent)
    {
        return !String.IsNullOrWhiteSpace(agent.TwilioPhoneNumber) ||
            !String.IsNullOrWhiteSpace(agent.TelegramBotToken) ||
            !String.IsNullOrWhiteSpace(agent.WhatsappNumber);
    }
}
