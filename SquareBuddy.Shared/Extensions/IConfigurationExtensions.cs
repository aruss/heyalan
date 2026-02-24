namespace Microsoft.Extensions.Configuration;

public static class IConfigurationExtensions
{
    public static TConfiguration BindTo<TConfiguration>(this IConfiguration configuration)
        where TConfiguration : new()
    {
        var configInstance = new TConfiguration();
        configuration.Bind(configInstance);
        return configInstance;
    }
}
