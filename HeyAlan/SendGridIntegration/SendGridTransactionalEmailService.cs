
namespace HeyAlan.SendGridIntegration;

using HeyAlan.Email;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public sealed class SendGridTransactionalEmailService : ITransactionalEmailService
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly SendGridOptions options;

    public SendGridTransactionalEmailService(
        IHttpClientFactory httpClientFactory,
        SendGridOptions options)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task SendTemplateAsync(
        string recipientEmail,
        string templateKey,
        IReadOnlyDictionary<string, string> templateData,
        CancellationToken cancellationToken = default)
    {
        string normalizedRecipientEmail = NormalizeRequiredValue(recipientEmail, nameof(recipientEmail), "Recipient email is required.");
        string normalizedTemplateKey = NormalizeRequiredValue(templateKey, nameof(templateKey), "Template id is required.");
        IReadOnlyDictionary<string, string> normalizedTemplateData = NormalizeRequiredTemplateData(templateData);

        string templateId = this.ResolveTemplateId(normalizedTemplateKey);

        HttpClient client = this.httpClientFactory.GetSendGridClient();

        using HttpRequestMessage request = this.CreateRequest(
            normalizedRecipientEmail,
            templateId,
            normalizedTemplateData);

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string responseBody = response.Content is null
            ? String.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException(
            $"SendGrid transactional email failed with status {(int)response.StatusCode}. ResponseLength={responseBody.Length}.");
    }

    private HttpRequestMessage CreateRequest(
        string recipientEmail,
        string templateId,
        IReadOnlyDictionary<string, string> templateData)
    {
        SendGridMailSendPayload payload = new(
            [new SendGridPersonalizationPayload(
                [new SendGridEmailAddressPayload(recipientEmail)],
                templateData)],
            new SendGridEmailAddressPayload(this.options.FromEmail),
            templateId);

        string payloadJson = JsonSerializer.Serialize(payload);

        HttpRequestMessage request = new(HttpMethod.Post, "/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.options.ApiKey);
        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        return request;
    }

    private static string NormalizeRequiredValue(string? value, string paramName, string message)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, paramName);
        }

        return value.Trim();
    }

    private static IReadOnlyDictionary<string, string> NormalizeRequiredTemplateData(
        IReadOnlyDictionary<string, string>? templateData)
    {
        if (templateData is null)
        {
            throw new ArgumentNullException(nameof(templateData));
        }

        Dictionary<string, string> normalizedTemplateData = [];

        foreach (KeyValuePair<string, string> pair in templateData)
        {
            if (String.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("Template data keys must be non-empty.", nameof(templateData));
            }

            if (pair.Value is null)
            {
                throw new ArgumentException(
                    $"Template data value for key '{pair.Key}' must be non-null.",
                    nameof(templateData));
            }

            normalizedTemplateData[pair.Key.Trim()] = pair.Value;
        }

        return normalizedTemplateData;
    }

    public string ResolveTemplateId(string templateKey)
    {
        if (String.IsNullOrWhiteSpace(templateKey))
        {
            throw new ArgumentException("Template key is required.", nameof(templateKey));
        }

        string normalizedTemplateKey = templateKey.Trim();

        return normalizedTemplateKey switch
        {
            EmailTemplateKey.IdentityConfirmationLink => this.options.IdentityConfirmationLinkTemplateId,
            EmailTemplateKey.IdentityPasswordResetLink => this.options.IdentityPasswordResetLinkTemplateId,
            EmailTemplateKey.IdentityPasswordResetCode => this.options.IdentityPasswordResetCodeTemplateId,
            EmailTemplateKey.NewsletterConfirmation => this.options.NewsletterConfirmationTemplateId,
            _ => throw new InvalidOperationException($"Template key '{normalizedTemplateKey}' is not configured.")
        };
    }


    private sealed record SendGridMailSendPayload(
        SendGridPersonalizationPayload[] personalizations,
        SendGridEmailAddressPayload from,
        string template_id);

    private sealed record SendGridPersonalizationPayload(
        SendGridEmailAddressPayload[] to,
        IReadOnlyDictionary<string, string> dynamic_template_data);

    private sealed record SendGridEmailAddressPayload(string email);
}
