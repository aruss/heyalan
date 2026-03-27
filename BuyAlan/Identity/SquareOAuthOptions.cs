namespace BuyAlan.Identity;

using Microsoft.AspNetCore.Authentication.OAuth;

internal sealed class SquareOAuthOptions : OAuthOptions
{
    public string BrokerRedirectUri { get; set; } = String.Empty;

    public bool IncludeSessionFalse { get; set; }
}
