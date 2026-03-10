namespace HeyAlan.WebApi.SquareIntegration;

using System.Text;
using System.Text.Json;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.SquareIntegration;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Square;

public static class SquareCatalogWebhookEndpoints
{
    private const string CatalogVersionUpdatedEventType = "catalog.version.updated";
    private const string SignatureHeaderName = "x-square-hmacsha256-signature";

    public static IEndpointRouteBuilder MapSquareCatalogWebhookEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapPost(SquareIntegrationRules.CatalogWebhookPath, IngestSquareCatalogWebhookAsync)
            .WithTags("Webhooks")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return routeBuilder;
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult>> IngestSquareCatalogWebhookAsync(
        HttpRequest request,
        MainDataContext dbContext,
        AppOptions appOptions,
        ISubscriptionCatalogSyncTriggerService triggerService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ILogger logger = loggerFactory.CreateLogger("HeyAlan.SquareCatalogWebhook");
        string requestBody = await ReadRequestBodyAsync(request, cancellationToken);

        if (!IsValidSignature(request, requestBody, appOptions))
        {
            logger.LogWarning(
                "Rejected Square catalog webhook request because the signature was invalid. Path: {Path}.",
                request.Path);
            return TypedResults.Unauthorized();
        }

        SquareWebhookEnvelope? envelope = TryParseEnvelope(requestBody, logger);
        if (envelope is null)
        {
            return TypedResults.Ok();
        }

        if (!String.Equals(envelope.EventType, CatalogVersionUpdatedEventType, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Ignored Square webhook event {EventType} with event id {EventId}.",
                envelope.EventType,
                envelope.EventId);
            return TypedResults.Ok();
        }

        Guid? subscriptionId = await dbContext.SubscriptionSquareConnections
            .Where(item =>
                item.SquareMerchantId == envelope.MerchantId &&
                item.DisconnectedAtUtc == null)
            .Select(item => (Guid?)item.SubscriptionId)
            .SingleOrDefaultAsync(cancellationToken);

        if (subscriptionId is null)
        {
            logger.LogInformation(
                "Ignored Square catalog webhook event {EventId} because no active subscription matched merchant id {MerchantId}.",
                envelope.EventId,
                envelope.MerchantId);
            return TypedResults.Ok();
        }

        SquareWebhookReceipt? existingReceipt = await dbContext.SquareWebhookReceipts
            .SingleOrDefaultAsync(item => item.EventId == envelope.EventId, cancellationToken);

        if (existingReceipt is not null && existingReceipt.IsProcessed)
        {
            logger.LogInformation(
                "Ignored duplicate Square catalog webhook event {EventId} for subscription {SubscriptionId}.",
                envelope.EventId,
                subscriptionId.Value);
            return TypedResults.Ok();
        }

        SquareWebhookReceipt receipt = existingReceipt ?? new SquareWebhookReceipt
        {
            SubscriptionId = subscriptionId.Value,
            EventId = envelope.EventId,
            EventType = envelope.EventType,
            MerchantId = envelope.MerchantId,
            ReceivedAtUtc = DateTime.UtcNow,
            IsProcessed = false
        };

        if (existingReceipt is null)
        {
            dbContext.SquareWebhookReceipts.Add(receipt);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                receipt = await dbContext.SquareWebhookReceipts
                    .SingleAsync(item => item.EventId == envelope.EventId, cancellationToken);

                if (receipt.IsProcessed)
                {
                    logger.LogInformation(
                        "Ignored duplicate Square catalog webhook event {EventId} for subscription {SubscriptionId} after receipt insert race.",
                        envelope.EventId,
                        subscriptionId.Value);
                    return TypedResults.Ok();
                }
            }
        }

        await triggerService.RequestSyncAsync(
            new SubscriptionCatalogSyncRequestInput(
                subscriptionId.Value,
                CatalogSyncTriggerSource.Webhook),
            cancellationToken);

        receipt.IsProcessed = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Accepted Square catalog webhook event {EventId} for subscription {SubscriptionId}.",
            envelope.EventId,
            subscriptionId.Value);

        return TypedResults.Ok();
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();

        using StreamReader reader = new(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        string requestBody = await reader.ReadToEndAsync(cancellationToken);
        request.Body.Position = 0;
        return requestBody;
    }

    private static bool IsValidSignature(HttpRequest request, string requestBody, AppOptions appOptions)
    {
        if (String.IsNullOrWhiteSpace(appOptions.SquareWebhookSignatureKey))
        {
            return false;
        }

        if (!request.Headers.TryGetValue(SignatureHeaderName, out var signatureHeaderValues))
        {
            return false;
        }

        string? signatureHeader = signatureHeaderValues.SingleOrDefault();
        if (String.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        string notificationUrl = BuildNotificationUrl(appOptions.PublicBaseUrl);
        return WebhooksHelper.VerifySignature(
            requestBody,
            signatureHeader,
            appOptions.SquareWebhookSignatureKey,
            notificationUrl);
    }

    private static string BuildNotificationUrl(Uri publicBaseUrl)
    {
        return new Uri(publicBaseUrl, SquareIntegrationRules.CatalogWebhookPath).ToString();
    }

    private static SquareWebhookEnvelope? TryParseEnvelope(string requestBody, ILogger logger)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(requestBody);
            JsonElement root = document.RootElement;

            string? eventId = ReadString(root, "event_id");
            string? eventType = ReadString(root, "type");
            string? merchantId = ReadString(root, "merchant_id");
            if (String.IsNullOrWhiteSpace(eventId) ||
                String.IsNullOrWhiteSpace(eventType) ||
                String.IsNullOrWhiteSpace(merchantId))
            {
                logger.LogWarning("Ignored Square webhook payload because required fields were missing.");
                return null;
            }

            return new SquareWebhookEnvelope(
                eventId,
                eventType,
                merchantId);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Ignored Square webhook payload because the JSON body was invalid.");
            return null;
        }
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private sealed record SquareWebhookEnvelope(
        string EventId,
        string EventType,
        string MerchantId);
}
