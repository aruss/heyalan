namespace BuyAlan.WebApi.SmsConsent;

public sealed record CreateSmsConsentInput(
    string? PhoneNumber,
    bool? TransactionalConsent,
    bool? MarketingConsent);
