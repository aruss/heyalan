namespace HeyAlan.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using HeyAlan.Configuration;
using HeyAlan.TelegramIntegration;
using System.Net;
using System.Text;
using System.Text.Json;
using Telegram.Bot.Exceptions;

public class TelegramServiceTests
{
    [Theory]
    [InlineData("https://heyalan.test")]
    [InlineData("https://heyalan.test/")]
    public async Task RegisterWebhookAsync_ComposesWebhookUrlFromAppOptions(string baseUrl)
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return CreateTelegramSuccessResponse();
        });

        TelegramService service = CreateService(handler, new Uri(baseUrl));

        await service.RegisterWebhookAsync("12345:token-value");

        JsonDocument payload = ParseRequestPayload(handler.LastRequestContent!);
        string webhookUrl = ReadRequiredString(payload.RootElement, "url");

        Assert.Equal("https://heyalan.test/webhooks/telegram/12345:token-value", webhookUrl);
    }

    [Fact]
    public async Task RegisterWebhookAsync_SendsSecretTokenAndAllowedUpdates()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return CreateTelegramSuccessResponse();
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        await service.RegisterWebhookAsync("12345:token-value");

        JsonDocument payload = ParseRequestPayload(handler.LastRequestContent!);
        string secretToken = ReadRequiredString(payload.RootElement, "secret_token");

        Assert.Equal("integration-secret", secretToken);

        JsonElement allowedUpdatesElement = payload.RootElement.GetProperty("allowed_updates");
        Assert.Equal(JsonValueKind.Array, allowedUpdatesElement.ValueKind);
        Assert.Contains(allowedUpdatesElement.EnumerateArray(), element => element.GetString() == "message");
    }

    [Fact]
    public async Task RegisterWebhookAsync_ThrowsOnEmptyToken()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return CreateTelegramSuccessResponse();
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterWebhookAsync("   "));
        Assert.Null(handler.LastRequestContent);
    }

    [Fact]
    public async Task RegisterWebhookAsync_BubblesTelegramApiFailures()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"ok\":false,\"error_code\":400,\"description\":\"Bad Request: invalid webhook URL\"}",
                    Encoding.UTF8,
                    "application/json")
            };

            return response;
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        await Assert.ThrowsAsync<RequestException>(() => service.RegisterWebhookAsync("12345:token-value"));
    }

    [Fact]
    public async Task SendMessageAsync_SendsChatIdAndText()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"ok\":true,\"result\":{\"message_id\":1,\"date\":1734567890,\"chat\":{\"id\":987654321,\"type\":\"private\"},\"text\":\"Hello from tests\"}}",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        await service.SendMessageAsync("12345:token-value", 987654321L, "Hello from tests");

        JsonDocument payload = ParseRequestPayload(handler.LastRequestContent!);
        long chatId = payload.RootElement.GetProperty("chat_id").GetInt64();
        string text = ReadRequiredString(payload.RootElement, "text");

        Assert.Equal(987654321L, chatId);
        Assert.Equal("Hello from tests", text);
    }

    [Fact]
    public async Task SendMessageAsync_ThrowsOnEmptyToken()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return CreateTelegramSuccessResponse();
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        await Assert.ThrowsAsync<ArgumentException>(() => service.SendMessageAsync("  ", 1234L, "hello"));
        Assert.Null(handler.LastRequestContent);
    }

    [Fact]
    public async Task SendMessageAsync_ThrowsOnEmptyText()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return CreateTelegramSuccessResponse();
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        await Assert.ThrowsAsync<ArgumentException>(() => service.SendMessageAsync("12345:token-value", 1234L, "   "));
        Assert.Null(handler.LastRequestContent);
    }

    [Fact]
    public async Task SendMessageAsync_BubblesTelegramApiFailures()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"ok\":false,\"error_code\":403,\"description\":\"Forbidden: bot was blocked by the user\"}",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        await Assert.ThrowsAsync<RequestException>(() => service.SendMessageAsync("12345:token-value", 1234L, "hello"));
    }

    [Fact]
    public async Task RegisterWebhookIfTokenChangedAsync_WhenTokenUnchanged_DoesNotAttemptRegistration()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return CreateTelegramSuccessResponse();
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        TelegramTokenRegistrationResult result = await service.RegisterWebhookIfTokenChangedAsync(
            "12345:token-value",
            "12345:token-value");

        Assert.False(result.WasAttempted);
        Assert.Null(result.ErrorCode);
        Assert.Null(handler.LastRequestContent);
    }

    [Fact]
    public async Task RegisterWebhookIfTokenChangedAsync_WhenTokenChangesAndRegistrationSucceeds_ReturnsSuccess()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return CreateTelegramSuccessResponse();
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        TelegramTokenRegistrationResult result = await service.RegisterWebhookIfTokenChangedAsync(
            "old-token",
            "12345:token-value");

        Assert.True(result.WasAttempted);
        Assert.Null(result.ErrorCode);
        Assert.NotNull(handler.LastRequestContent);
    }

    [Fact]
    public async Task RegisterWebhookIfTokenChangedAsync_WhenTokenChangesAndTelegramReturnsUnauthorized_ReturnsInvalidTokenCode()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(
                    "{\"ok\":false,\"error_code\":401,\"description\":\"Unauthorized\"}",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        TelegramTokenRegistrationResult result = await service.RegisterWebhookIfTokenChangedAsync(
            "old-token",
            "12345:token-value");

        Assert.True(result.WasAttempted);
        Assert.Equal("telegram_bot_token_invalid", result.ErrorCode);
    }

    [Fact]
    public async Task RegisterWebhookIfTokenChangedAsync_WhenTokenChangesAndTransportFails_ReturnsWebhookFailureCode()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            throw new HttpRequestException("Network error.");
        });

        TelegramService service = CreateService(handler, new Uri("https://heyalan.test"));

        TelegramTokenRegistrationResult result = await service.RegisterWebhookIfTokenChangedAsync(
            "old-token",
            "12345:token-value");

        Assert.True(result.WasAttempted);
        Assert.Equal("telegram_webhook_registration_failed", result.ErrorCode);
    }

    private static TelegramService CreateService(RecordingHandler handler, Uri baseUrl)
    {
        FakeHttpClientFactory clientFactory = new(handler);
        TelegramClientFactory telegramClientFactory = new(clientFactory);
        TelegramOptions telegramOptions = new()
        {
            SecretToken = "integration-secret"
        };

        AppOptions appOptions = new()
        {
            PublicBaseUrl = baseUrl
        };

        return new TelegramService(
            telegramClientFactory,
            telegramOptions,
            appOptions,
            NullLogger<TelegramService>.Instance);
    }

    private static HttpResponseMessage CreateTelegramSuccessResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true,\"result\":true}", Encoding.UTF8, "application/json")
        };
    }

    private static JsonDocument ParseRequestPayload(string payload)
    {
        return JsonDocument.Parse(payload);
    }

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        JsonElement value = root.GetProperty(propertyName);
        return value.GetString() ?? String.Empty;
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            this.client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.telegram.org")
            };
        }

        public HttpClient CreateClient(string name)
        {
            return this.client;
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        public string? LastRequestContent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequestContent = request.Content is null
                ? String.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            HttpResponseMessage response = this.responseFactory(request);
            return response;
        }
    }
}
