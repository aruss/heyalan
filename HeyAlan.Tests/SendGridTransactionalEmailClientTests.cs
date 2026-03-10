namespace HeyAlan.Tests;

using HeyAlan.SendGridIntegration;
using System.Net;
using System.Text;
using System.Text.Json;

public class SendGridTransactionalEmailClientTests
{
    [Fact]
    public async Task SendTemplateAsync_SendsExpectedPayload()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        FakeHttpClientFactory httpClientFactory = new(handler);
        SendGridEmailOptions options = new()
        {
            ApiKey = "sendgrid-api-key",
            FromEmail = "notifications@heyalan.app",
            IdentityConfirmationLinkTemplateId = "d-confirm",
            IdentityPasswordResetLinkTemplateId = "d-reset-link",
            IdentityPasswordResetCodeTemplateId = "d-reset-code",
            NewsletterConfirmationTemplateId = "d-newsletter"
        };

        SendGridTransactionalEmailClient client = new(httpClientFactory, options);

        await client.SendTemplateAsync(
            "person@example.com",
            "d-confirm",
            new Dictionary<string, string>
            {
                ["confirmation_url"] = "https://heyalan.app/confirm?token=abc"
            });

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://api.sendgrid.com/v3/mail/send", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("sendgrid-api-key", handler.LastRequest.Headers.Authorization?.Parameter);

        JsonDocument payload = JsonDocument.Parse(handler.LastRequestContent!);
        JsonElement root = payload.RootElement;
        Assert.Equal("d-confirm", root.GetProperty("template_id").GetString());
        Assert.Equal("notifications@heyalan.app", root.GetProperty("from").GetProperty("email").GetString());

        JsonElement firstPersonalization = root.GetProperty("personalizations")[0];
        Assert.Equal("person@example.com", firstPersonalization.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal(
            "https://heyalan.app/confirm?token=abc",
            firstPersonalization.GetProperty("dynamic_template_data").GetProperty("confirmation_url").GetString());
    }

    [Fact]
    public async Task SendTemplateAsync_WhenSendGridFails_Throws()
    {
        RecordingHandler handler = new((HttpRequestMessage _) =>
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"errors\":[{\"message\":\"bad request\"}]}", Encoding.UTF8, "application/json")
            };
        });

        FakeHttpClientFactory httpClientFactory = new(handler);
        SendGridEmailOptions options = new()
        {
            ApiKey = "sendgrid-api-key",
            FromEmail = "notifications@heyalan.app",
            IdentityConfirmationLinkTemplateId = "d-confirm",
            IdentityPasswordResetLinkTemplateId = "d-reset-link",
            IdentityPasswordResetCodeTemplateId = "d-reset-code",
            NewsletterConfirmationTemplateId = "d-newsletter"
        };

        SendGridTransactionalEmailClient client = new(httpClientFactory, options);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SendTemplateAsync(
                "person@example.com",
                "d-confirm",
                new Dictionary<string, string>
                {
                    ["confirmation_url"] = "https://heyalan.app/confirm?token=abc"
                }));
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            this.client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.sendgrid.com")
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

        public string? LastRequestContent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequest = request;
            this.LastRequestContent = request.Content is null
                ? String.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            HttpResponseMessage response = this.responseFactory(request);
            return response;
        }
    }
}
