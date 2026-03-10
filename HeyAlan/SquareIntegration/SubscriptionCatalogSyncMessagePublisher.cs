namespace HeyAlan.SquareIntegration;

using Wolverine;

public sealed class SubscriptionCatalogSyncMessagePublisher : ISubscriptionCatalogSyncMessagePublisher
{
    private readonly IMessageBus messageBus;

    public SubscriptionCatalogSyncMessagePublisher(IMessageBus messageBus)
    {
        this.messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    }

    public Task PublishAsync(SquareCatalogSyncRequested message, CancellationToken cancellationToken = default)
    {
        return this.messageBus.SendAsync(message).AsTask();
    }
}
