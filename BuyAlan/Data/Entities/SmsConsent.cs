namespace BuyAlan.Data.Entities;

public sealed class SmsConsent : IEntityWithId, IEntityWithAudit
{
    public Guid Id { get; set; }

    public string PhoneNumber { get; set; } = String.Empty;

    public bool TransactionalConsent { get; set; }

    public bool MarketingConsent { get; set; }

    public string ConsentSource { get; set; } = String.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
