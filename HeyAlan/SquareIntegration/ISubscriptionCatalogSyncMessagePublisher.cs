namespace HeyAlan.SquareIntegration;

public interface ISubscriptionCatalogSyncMessagePublisher
{
    Task PublishAsync(SquareCatalogSyncRequested message, CancellationToken cancellationToken = default);
}
