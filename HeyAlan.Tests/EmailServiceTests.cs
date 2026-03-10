namespace HeyAlan.Tests;

using HeyAlan.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Wolverine;

public class EmailServiceTests
{
    [Fact]
    public async Task EnqueueAsync_WhenMessageValid_SendsNormalizedMessage()
    {
        RecordingMessageBus messageBus = new();
        EmailService service = new(messageBus, NullLogger<EmailService>.Instance);

        await service.EnqueueAsync(new EmailSendRequested(
            " person@example.com ",
            $" {EmailTemplateKey.IdentityConfirmationLink} ",
            new Dictionary<string, string>
            {
                [" confirmation_url "] = "https://heyalan.test/confirm"
            }));

        Assert.NotNull(messageBus.LastSentMessage);
        Assert.Equal("person@example.com", messageBus.LastSentMessage!.RecipientEmail);
        Assert.Equal(EmailTemplateKey.IdentityConfirmationLink, messageBus.LastSentMessage.TemplateKey);
        Assert.Equal("https://heyalan.test/confirm", messageBus.LastSentMessage.TemplateData["confirmation_url"]);
    }

    [Fact]
    public async Task EnqueueAsync_WhenTemplateKeyUnsupported_Throws()
    {
        RecordingMessageBus messageBus = new();
        EmailService service = new(messageBus, NullLogger<EmailService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.EnqueueAsync(new EmailSendRequested(
            "person@example.com",
            "missing-template",
            new Dictionary<string, string>())));
    }

    private sealed class RecordingMessageBus : IMessageBus
    {
        public EmailSendRequested? LastSentMessage { get; private set; }
        public string? TenantId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ValueTask BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public IDestinationEndpoint EndpointFor(string endpointName)
        {
            throw new NotImplementedException();
        }

        public IDestinationEndpoint EndpointFor(Uri uri)
        {
            throw new NotImplementedException();
        }

        public Task InvokeAsync(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public Task InvokeAsync(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> InvokeAsync<T>(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> InvokeAsync<T>(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public Task InvokeForTenantAsync(string tenantId, object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> InvokeForTenantAsync<T>(string tenantId, object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<Envelope> PreviewSubscriptions(object message)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<Envelope> PreviewSubscriptions(object message, DeliveryOptions options)
        {
            throw new NotImplementedException();
        }

        public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
        {
            this.LastSentMessage = message as EmailSendRequested;
            return ValueTask.CompletedTask;
        }
    }
    
}
