namespace HeyAlan.Data.Entities;

public class SubscriptionSquareConnection : IEntityWithAudit
{
    public Guid SubscriptionId { get; set; }

    public Subscription Subscription { get; set; } = null!;

    public string SquareMerchantId { get; set; } = null!;

    public string EncryptedAccessToken { get; set; } = null!;

    public string EncryptedRefreshToken { get; set; } = null!;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string Scopes { get; set; } = null!;

    public Guid ConnectedByUserId { get; set; }

    public ApplicationUser ConnectedByUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DisconnectedAtUtc { get; set; }
}
