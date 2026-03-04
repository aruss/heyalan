namespace HeyAlan.Messaging;

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;
using HeyAlan.Data;
using HeyAlan.Data.Entities;

public class IncomingMessageConsumer
{
    private static readonly TimeSpan BufferQuietWindow = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FakeBusinessLogicDuration = TimeSpan.FromSeconds(4);
    private const int MaxBufferedMessagesPerBatch = 100;

    private static readonly ConcurrentDictionary<ConversationKey, ConversationBufferState> BufferStates = new();

    private readonly ILogger<IncomingMessageConsumer> logger;
    private readonly IMessageBus messageBus;
    private readonly IConversationStore conversationStore;
    private readonly MainDataContext dbContext;

    public IncomingMessageConsumer(
        ILogger<IncomingMessageConsumer> logger,
        IMessageBus messageBus,
        IConversationStore conversationStore,
        MainDataContext dbContext)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        this.conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task Consume(IncomingMessage message, CancellationToken ct)
    {
        ConversationKey key = new(message.AgentId, message.Channel, message.From);
        ConversationBufferState state = BufferStates.GetOrAdd(key, static _ => new ConversationBufferState());
        BufferedEnvelope envelope = new(message);
        bool runCoordinator = false;

        lock (state.SyncRoot)
        {
            state.PendingMessages.Add(envelope);
            state.LastMessageReceivedAt = DateTimeOffset.UtcNow;

            this.logger.LogInformation(
                "Buffered message for Subscription {SubscriptionId}, Agent {AgentId}, Channel {Channel}, From {From}. Pending count: {PendingCount}.",
                message.SubscriptionId,
                message.AgentId,
                message.Channel,
                message.From,
                state.PendingMessages.Count);

            state.ActiveLongRunningTaskCancellationTokenSource?.Cancel();

            if (!state.IsCoordinatorRunning)
            {
                state.IsCoordinatorRunning = true;
                runCoordinator = true;
            }
        }

        if (runCoordinator)
        {
            await this.RunCoordinatorAsync(key, state, ct);
        }

        await envelope.Completion.Task.WaitAsync(ct);
    }

    private async Task RunCoordinatorAsync(
        ConversationKey key,
        ConversationBufferState state,
        CancellationToken shutdownToken)
    {
        List<BufferedEnvelope> currentBatch = [];

        try
        {
            while (true)
            {
                await this.WaitForQuietWindowAsync(state, shutdownToken);

                List<BufferedEnvelope> batch = this.TakeNextBatch(state);
                if (batch.Count == 0)
                {
                    return;
                }

                Agent agent = await this.LoadAgentAsync(batch, shutdownToken);

                currentBatch = batch;
                LongRunningTaskOutcome outcome = await this.RunLongRunningTaskAsync(state, key, batch, agent, shutdownToken);
                if (outcome.WasCanceled)
                {
                    currentBatch = [];
                    continue;
                }

                await this.PersistBatchAsync(batch, shutdownToken);
                await this.SendBatchReplyAsync(outcome.Result, shutdownToken);

                foreach (BufferedEnvelope completedEnvelope in batch)
                {
                    completedEnvelope.Completion.TrySetResult();
                }

                currentBatch = [];
            }
        }
        catch (Exception exception)
        {
            foreach (BufferedEnvelope envelope in currentBatch)
            {
                envelope.Completion.TrySetException(exception);
            }

            List<BufferedEnvelope> envelopesToFail = this.TakeAllPendingMessages(state);

            foreach (BufferedEnvelope envelope in envelopesToFail)
            {
                envelope.Completion.TrySetException(exception);
            }

            throw;
        }
        finally
        {
            lock (state.SyncRoot)
            {
                state.IsCoordinatorRunning = false;
                state.ActiveLongRunningTaskCancellationTokenSource?.Dispose();
                state.ActiveLongRunningTaskCancellationTokenSource = null;
            }
        }
    }

    private async Task WaitForQuietWindowAsync(ConversationBufferState state, CancellationToken ct)
    {
        while (true)
        {
            DateTimeOffset lastArrival;
            lock (state.SyncRoot)
            {
                lastArrival = state.LastMessageReceivedAt;
            }

            TimeSpan elapsed = DateTimeOffset.UtcNow - lastArrival;
            TimeSpan remaining = BufferQuietWindow - elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            await Task.Delay(remaining, ct);
        }
    }

    private List<BufferedEnvelope> TakeNextBatch(ConversationBufferState state)
    {
        lock (state.SyncRoot)
        {
            if (state.PendingMessages.Count == 0)
            {
                return [];
            }

            int batchCount = Math.Min(MaxBufferedMessagesPerBatch, state.PendingMessages.Count);
            List<BufferedEnvelope> batch = state.PendingMessages.GetRange(0, batchCount);
            state.PendingMessages.RemoveRange(0, batchCount);

            if (state.PendingMessages.Count > 0)
            {
                this.logger.LogInformation(
                    "Batch limit reached at {BatchLimit}. {RemainingCount} message(s) remain queued for the next processing cycle.",
                    MaxBufferedMessagesPerBatch,
                    state.PendingMessages.Count);
            }

            return batch;
        }
    }

    private async Task<Agent> LoadAgentAsync(List<BufferedEnvelope> batch, CancellationToken ct)
    {
        IncomingMessage firstMessage = batch[0].Message;

        Agent? agent = await this.dbContext.Agents
            .SingleOrDefaultAsync(item => item.Id == firstMessage.AgentId, ct);

        if (agent is null)
        {
            throw new InvalidOperationException(
                $"Agent '{firstMessage.AgentId}' was not found while processing incoming message batch.");
        }

        return agent;
    }

    private async Task<LongRunningTaskOutcome> RunLongRunningTaskAsync(
        ConversationBufferState state,
        ConversationKey key,
        List<BufferedEnvelope> batch,
        Agent agent,
        CancellationToken shutdownToken)
    {
        CancellationTokenSource processingTokenSource;
        lock (state.SyncRoot)
        {
            state.ActiveLongRunningTaskCancellationTokenSource?.Dispose();
            state.ActiveLongRunningTaskCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
            processingTokenSource = state.ActiveLongRunningTaskCancellationTokenSource;
        }

        try
        {
            this.logger.LogInformation(
                "Starting long-running batch task for Agent {AgentId}, Channel {Channel}, From {From}, Batch size {BatchSize}.",
                key.AgentId,
                key.Channel,
                key.From,
                batch.Count);

            await Task.Delay(FakeBusinessLogicDuration, processingTokenSource.Token);

            IncomingMessage firstMessage = batch[0].Message;
            BatchProcessingResult result = new(
                firstMessage.SubscriptionId,
                firstMessage.AgentId,
                firstMessage.Channel,
                firstMessage.From,
                batch.Count,
                $"{agent.Name} here, I got {batch.Count} messages");

            return new LongRunningTaskOutcome(false, result);
        }
        catch (OperationCanceledException) when (!shutdownToken.IsCancellationRequested)
        {
            this.logger.LogInformation(
                "Long-running task canceled for Agent {AgentId}, Channel {Channel}, From {From}. Re-queueing batch and waiting for a fresh quiet window.",
                key.AgentId,
                key.Channel,
                key.From);

            lock (state.SyncRoot)
            {
                state.PendingMessages.InsertRange(0, batch);
            }

            return new LongRunningTaskOutcome(true, null);
        }
        finally
        {
            lock (state.SyncRoot)
            {
                state.ActiveLongRunningTaskCancellationTokenSource?.Dispose();
                state.ActiveLongRunningTaskCancellationTokenSource = null;
            }
        }
    }

    private async Task PersistBatchAsync(List<BufferedEnvelope> batch, CancellationToken ct)
    {
        foreach (BufferedEnvelope envelope in batch)
        {
            await this.conversationStore.UpsertIncomingMessageAsync(envelope.Message, ct);
        }
    }

    private async Task SendBatchReplyAsync(BatchProcessingResult? result, CancellationToken ct)
    {
        if (result is null)
        {
            return;
        }

        if (result.Channel == MessageChannel.Telegram)
        {
            OutgoingTelegramMessage telegramMessage = new()
            {
                SubscriptionId = result.SubscriptionId,
                AgentId = result.AgentId,
                Content = result.Content,
                To = result.To
            };

            await this.messageBus.PublishAsync(telegramMessage);
            return;
        }

        this.logger.LogInformation(
            "No outgoing batch reply handler implemented for channel {Channel}. Batch size {BatchSize} persisted without outbound send.",
            result.Channel,
            result.MessageCount);
    }

    private List<BufferedEnvelope> TakeAllPendingMessages(ConversationBufferState state)
    {
        lock (state.SyncRoot)
        {
            List<BufferedEnvelope> messages = [.. state.PendingMessages];
            state.PendingMessages.Clear();
            return messages;
        }
    }

    private sealed record ConversationKey(Guid AgentId, MessageChannel Channel, string From);

    private sealed record BatchProcessingResult(
        Guid SubscriptionId,
        Guid AgentId,
        MessageChannel Channel,
        string To,
        int MessageCount,
        string Content);

    private sealed record LongRunningTaskOutcome(bool WasCanceled, BatchProcessingResult? Result);

    private sealed class BufferedEnvelope
    {
        public BufferedEnvelope(IncomingMessage message)
        {
            this.Message = message ?? throw new ArgumentNullException(nameof(message));
            this.Completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public IncomingMessage Message { get; }

        public TaskCompletionSource Completion { get; }
    }

    private sealed class ConversationBufferState
    {
        public object SyncRoot { get; } = new();

        public List<BufferedEnvelope> PendingMessages { get; } = [];

        public DateTimeOffset LastMessageReceivedAt { get; set; } = DateTimeOffset.UtcNow;

        public CancellationTokenSource? ActiveLongRunningTaskCancellationTokenSource { get; set; }

        public bool IsCoordinatorRunning { get; set; }
    }
}
