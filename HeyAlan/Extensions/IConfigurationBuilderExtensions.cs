using HeyAlan.Configuration;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

public static class IConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddEnvFile(this IConfigurationBuilder builder, string path)
    {
        if (!File.Exists(path))
        {
            return builder;
        }

        IDictionary<string, string>? entries = File.ReadAllLines(path)
            .Select(l => l.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

        return builder.AddInMemoryCollection(entries!);
    }


    [DebuggerStepThrough]
    public static string GetTrimmedValue(this IConfiguration configuration, string key, bool required = false)
    {
        string rawValue = configuration[key];

        if (required && String.IsNullOrWhiteSpace(rawValue))
        {
            throw ConfigurationErrors.Missing(key);
        }

        string normalizedValue = rawValue.Trim();
        if (String.IsNullOrWhiteSpace(normalizedValue))
        {
            throw ConfigurationErrors.Invalid(key);
        }

        return normalizedValue;
    }
}
