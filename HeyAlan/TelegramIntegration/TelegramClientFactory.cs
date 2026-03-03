namespace HeyAlan.TelegramIntegration;

using System.Collections.Concurrent;
using Telegram.Bot;

public class TelegramClientFactory
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ConcurrentDictionary<string, ITelegramBotClient> clients = new();

    public TelegramClientFactory(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public ITelegramBotClient GetClient(string botToken)
    {
        return this.clients.GetOrAdd(botToken, token =>
        {
            HttpClient httpClient = this.httpClientFactory.CreateClient("TelegramBotClient");
            return new TelegramBotClient(token, httpClient);
        });
    }
}
