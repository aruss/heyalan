namespace HeyAlan.TelegramIntegration;

using Microsoft.Extensions.Logging;

public sealed class TelegramSecretTokenFilter : IEndpointFilter
{
    private const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";
    private readonly string expectedToken;

    public TelegramSecretTokenFilter(string expectedToken)
    {
        this.expectedToken = expectedToken ?? throw new ArgumentNullException(nameof(expectedToken));
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ILogger<TelegramSecretTokenFilter> logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<TelegramSecretTokenFilter>>();

        if (!context.HttpContext.Request.Headers.TryGetValue(SecretTokenHeader, out var extractedToken) ||
            !string.Equals(extractedToken, this.expectedToken, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Rejected Telegram webhook request with invalid secret token. Path: {Path}.",
                context.HttpContext.Request.Path);

            return Results.Unauthorized();
        }

        return await next(context);
    }
}
