namespace HeyAlan.Configuration;

public static class ConfigurationErrors
{
    public static InvalidOperationException Missing(string key) =>
        new($"Missing config: {key}");

    public static InvalidOperationException Invalid(string key) =>
        new($"Invalid config: {key}");
}
