namespace BuyAlan.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

internal class SquareOAuthHandler : OAuthHandler<SquareOAuthOptions>
{
    public SquareOAuthHandler(
        IOptionsMonitor<SquareOAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
    {
        string brokerRedirectUri = GetRequiredBrokerRedirectUri(this.Options);
        string authorizationUrl = base.BuildChallengeUrl(properties, brokerRedirectUri);

        if (!this.Options.IncludeSessionFalse)
        {
            return authorizationUrl;
        }

        return QueryHelpers.AddQueryString(authorizationUrl, "session", "false");
    }

    protected override Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
    {
        string brokerRedirectUri = GetRequiredBrokerRedirectUri(this.Options);
        OAuthCodeExchangeContext rewrittenContext = CreateBrokerCodeExchangeContext(
            context.Properties,
            context.Code,
            brokerRedirectUri);

        return base.ExchangeCodeAsync(rewrittenContext);
    }

    internal static OAuthCodeExchangeContext CreateBrokerCodeExchangeContext(
        AuthenticationProperties properties,
        string code,
        string brokerRedirectUri)
    {
        return new OAuthCodeExchangeContext(properties, code, brokerRedirectUri);
    }

    internal static string GetRequiredBrokerRedirectUri(SquareOAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (String.IsNullOrWhiteSpace(options.BrokerRedirectUri))
        {
            throw new InvalidOperationException("Square broker redirect URI must be configured.");
        }

        return options.BrokerRedirectUri;
    }
}
