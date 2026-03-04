namespace HeyAlan.Messaging;

using System.Net;
using System.Net.Sockets;

public static class RabbitMqStartupGuard
{
    private const int DefaultRabbitPort = 5672;

    public static void EnsureReachable(string connectionString, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("RabbitMQ connection string is missing.");
        }

        (string host, int port) = ParseEndpoint(connectionString);

        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0)
            {
                throw new InvalidOperationException(
                    $"RabbitMQ host '{host}' resolved to zero addresses.");
            }
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"RabbitMQ DNS resolution failed for host '{host}'.",
                exception);
        }

        using TcpClient client = new();
        using CancellationTokenSource timeoutCts = new(timeout);

        try
        {
            client.ConnectAsync(host, port, timeoutCts.Token).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"RabbitMQ connectivity check failed for endpoint '{host}:{port}'.",
                exception);
        }
    }

    private static (string Host, int Port) ParseEndpoint(string connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase)))
        {
            int uriPort = uri.IsDefaultPort ? DefaultRabbitPort : uri.Port;
            return (uri.Host, uriPort);
        }

        Dictionary<string, string> parts = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(item => item.Length == 2)
            .ToDictionary(
                item => item[0],
                item => item[1],
                StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("host", out string? host) &&
            !parts.TryGetValue("hostname", out host) &&
            !parts.TryGetValue("server", out host))
        {
            throw new InvalidOperationException(
                "RabbitMQ connection string must contain a host/hostname/server value.");
        }

        int port = DefaultRabbitPort;
        if (parts.TryGetValue("port", out string? portRaw) &&
            int.TryParse(portRaw, out int parsedPort) &&
            parsedPort > 0)
        {
            port = parsedPort;
        }

        string normalizedHost = host.Trim();
        if (normalizedHost.Contains(',', StringComparison.Ordinal))
        {
            normalizedHost = normalizedHost.Split(',', 2, StringSplitOptions.TrimEntries)[0];
        }

        return (normalizedHost, port);
    }
}
