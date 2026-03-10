namespace HeyAlan.SendGridIntegration;

using HeyAlan.Email;
using HeyAlan.Newsletter;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public sealed class SendGridNewsletterUpsertService : INewsletterUpsertService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly SendGridOptions options;

    public SendGridNewsletterUpsertService(
        IHttpClientFactory httpClientFactory,
        SendGridOptions options)
    {
        this.httpClientFactory = httpClientFactory ??
            throw new ArgumentNullException(nameof(httpClientFactory));

        this.options = options ?? 
            throw new ArgumentNullException(nameof(options));
    }

    public async Task UpsertNewsletterContactAsync(
        string email, 
        CancellationToken cancellationToken = default)
    {
        if (String.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        HttpClient client = this.httpClientFactory.CreateClient(SendGridBuilderExtensions.SendGridClientName);
        using HttpRequestMessage request = this.CreateUpsertNewsletterContactRequest(email.Trim());

        using HttpResponseMessage response = 
            await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string responseBody = response.Content is null
            ? String.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException(
            $"SendGrid contact upsert failed with status {(int)response.StatusCode}: {responseBody}");
    }

    private HttpRequestMessage CreateUpsertNewsletterContactRequest(string email)
    {
        SendGridUpsertContactsPayload payload = new(
            [this.options.NewsletterListId],
            [new SendGridContactPayload(email)]);

        string payloadJson = JsonSerializer.Serialize(payload);

        HttpRequestMessage request = 
            new(HttpMethod.Put, "/v3/marketing/contacts");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", this.options.ApiKey);

        request.Content =
            new StringContent(payloadJson, Encoding.UTF8, "application/json");

        return request;
    }
    private sealed record SendGridUpsertContactsPayload(
        string[] list_ids,
        SendGridContactPayload[] contacts);

    private sealed record SendGridContactPayload(string email);
}
