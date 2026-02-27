namespace ShelfBuddy.TelegramIntegration;

public sealed class TelegramSecretTokenFilter(string expectedToken) : IEndpointFilter
{
    private const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(SecretTokenHeader, out var extractedToken) ||
            !string.Equals(extractedToken, expectedToken, StringComparison.Ordinal))
        {
            // Drop unauthorized traffic immediately
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
