namespace SquareBuddy.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using SquareBuddy.Configuration;
using SquareBuddy.TelegramIntegration;
using System.Net;
using System.Text;
using System.Text.Json;
using Telegram.Bot.Exceptions;

public class TelegramServiceTests
{
    [Theory]
    [InlineData("https://squarebuddy.test")]
    [InlineData("https://squarebuddy.test/")]
    public async Task RegisterWebhookAsync_ComposesWebhookUrlFromAppOptions(string baseUrl)
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return CreateTelegramSuccessResponse();
        });

        TelegramService service = CreateService(handler, new Uri(baseUrl));

        await service.RegisterWebhookAsync("12345:token-value");

        JsonDocument payload = ParseRequestPayload(handler.LastRequest!);
        string webhookUrl = ReadRequiredString(payload.RootElement, "url");

        Assert.Equal("https://squarebuddy.test/webhooks/telegram/12345:token-value", webhookUrl);
    }

    [Fact]
    public async Task RegisterWebhookAsync_SendsSecretTokenAndAllowedUpdates()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return CreateTelegramSuccessResponse();
        });

        TelegramService service = CreateService(handler, new Uri("https://squarebuddy.test"));

        await service.RegisterWebhookAsync("12345:token-value");

        JsonDocument payload = ParseRequestPayload(handler.LastRequest!);
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

        TelegramService service = CreateService(handler, new Uri("https://squarebuddy.test"));

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterWebhookAsync("   "));
        Assert.Null(handler.LastRequest);
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

        TelegramService service = CreateService(handler, new Uri("https://squarebuddy.test"));

        await Assert.ThrowsAsync<ApiRequestException>(() => service.RegisterWebhookAsync("12345:token-value"));
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

    private static JsonDocument ParseRequestPayload(HttpRequestMessage request)
    {
        string payload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
        return JsonDocument.Parse(payload);
    }

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        JsonElement value = root.GetProperty(propertyName);
        return value.GetString() ?? string.Empty;
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

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequest = request;
            HttpResponseMessage response = this.responseFactory(request);
            return Task.FromResult(response);
        }
    }
}
