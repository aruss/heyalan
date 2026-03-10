namespace HeyAlan.Email;

public static class EmailTemplateKey
{
    public const string IdentityConfirmationLink = "identity_confirmation_link";
    public const string IdentityPasswordResetLink = "identity_password_reset_link";
    public const string IdentityPasswordResetCode = "identity_password_reset_code";
    public const string NewsletterConfirmation = "newsletter_confirmation";
    public const string Generic = "generic";

    private static readonly HashSet<string> AllKeys =
    [
        IdentityConfirmationLink,
        IdentityPasswordResetLink,
        IdentityPasswordResetCode,
        NewsletterConfirmation,
        Generic
    ];

    public static bool IsSupported(string? templateKey)
    {
        if (String.IsNullOrWhiteSpace(templateKey))
        {
            return false;
        }

        return AllKeys.Contains(templateKey.Trim());
    }
}
