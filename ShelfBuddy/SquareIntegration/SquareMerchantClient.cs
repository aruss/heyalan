namespace ShelfBuddy.SquareIntegration;

using ShelfBuddy.Configuration;
using Square;
using Square.Merchants;

public sealed class SquareMerchantClient : ISquareMerchantClient
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly AppOptions appOptions;

    public SquareMerchantClient(IHttpClientFactory httpClientFactory, AppOptions appOptions)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.appOptions = appOptions ?? throw new ArgumentNullException(nameof(appOptions));
    }

    public async Task<SquareMerchantProfileResult> GetMerchantProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new SquareMerchantProfileResult.ReconnectRequired();
        }

        if (string.IsNullOrWhiteSpace(this.appOptions.SquareClientId))
        {
            return new SquareMerchantProfileResult.Failure("square_not_configured");
        }

        string baseUrl = this.appOptions.SquareClientId.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase)
            ? "https://connect.squareupsandbox.com"
            : "https://connect.squareup.com";

        ClientOptions options = new()
        {
            BaseUrl = baseUrl,
            HttpClient = this.httpClientFactory.CreateClient("SquareApiClient")
        };

        SquareClient client = new(accessToken, clientOptions: options);

        try
        {
            GetMerchantResponse response = await client.Merchants.GetAsync(
                new GetMerchantsRequest { MerchantId = "me" },
                null,
                cancellationToken);

            if (response.Merchant is null || string.IsNullOrWhiteSpace(response.Merchant.Id))
            {
                return new SquareMerchantProfileResult.Failure("square_probe_failed");
            }

            return new SquareMerchantProfileResult.Success(new SquareMerchantProfile(response.Merchant.Id));
        }
        catch (SquareApiException exception)
        {
            if (exception.StatusCode == 401)
            {
                return new SquareMerchantProfileResult.ReconnectRequired();
            }

            return new SquareMerchantProfileResult.Failure("square_probe_failed");
        }
    }
}
