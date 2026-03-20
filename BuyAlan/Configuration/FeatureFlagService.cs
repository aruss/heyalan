namespace BuyAlan.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public static class FeatureFlagNames
{
    public const string LandingPricing = "landingPricing";
    public const string TeamMembers = "teamMembers";
}

public interface IFeatureFlagService
{
    bool IsFeatureEnabled(string featureName);
}

public sealed class FeatureFlagService : IFeatureFlagService
{
    private const string FeatureFlagsEnvironmentKey = "FEATURE_FLAGS";
    private const string FeatureFlagEntrySeparator = ";";
    private const char FeatureFlagKeyValueSeparator = '=';

    private static readonly IReadOnlyDictionary<string, bool> DefaultFeatureFlags =
        new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [FeatureFlagNames.LandingPricing] = false,
            [FeatureFlagNames.TeamMembers] = true,
        };

    private readonly ILogger<FeatureFlagService> logger;
    private readonly IReadOnlyDictionary<string, bool> parsedFeatureFlags;

    public FeatureFlagService(
        IConfiguration configuration,
        ILogger<FeatureFlagService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(configuration);

        this.parsedFeatureFlags = this.ParseFeatureFlags(configuration[FeatureFlagsEnvironmentKey]);
    }

    public bool IsFeatureEnabled(string featureName)
    {
        string? normalizedFeatureName = featureName.TrimToNull();
        if (normalizedFeatureName is null)
        {
            return false;
        }

        if (this.parsedFeatureFlags.TryGetValue(normalizedFeatureName, out bool parsedValue))
        {
            return parsedValue;
        }

        if (DefaultFeatureFlags.TryGetValue(normalizedFeatureName, out bool defaultValue))
        {
            return defaultValue;
        }

        return false;
    }

    private Dictionary<string, bool> ParseFeatureFlags(string? rawFeatureFlags)
    {
        Dictionary<string, bool> parsedFeatureFlags = new(StringComparer.Ordinal);
        string? normalizedFeatureFlags = rawFeatureFlags.TrimToNull();
        if (normalizedFeatureFlags is null)
        {
            return parsedFeatureFlags;
        }

        string[] rawSegments = normalizedFeatureFlags.Split(
            FeatureFlagEntrySeparator,
            StringSplitOptions.None);

        foreach (string rawSegment in rawSegments)
        {
            string? trimmedSegment = rawSegment.TrimToNull();
            if (trimmedSegment is null)
            {
                continue;
            }

            int separatorIndex = trimmedSegment.IndexOf(FeatureFlagKeyValueSeparator, StringComparison.Ordinal);
            int lastSeparatorIndex = trimmedSegment.LastIndexOf(FeatureFlagKeyValueSeparator);
            if (separatorIndex <= 0 || separatorIndex != lastSeparatorIndex)
            {
                this.logger.LogWarning(
                    "Ignoring invalid feature flag segment for {FeatureFlagsEnvironmentKey}. Reason: Expected {ExpectedTrueFormat} or {ExpectedFalseFormat}.",
                    FeatureFlagsEnvironmentKey,
                    "featureName=1",
                    "featureName=0");
                continue;
            }

            string rawKey = trimmedSegment[..separatorIndex].Trim();
            string rawValue = trimmedSegment[(separatorIndex + 1)..].Trim();

            if (!DefaultFeatureFlags.ContainsKey(rawKey))
            {
                this.logger.LogWarning(
                    "Ignoring unknown feature flag key {FeatureFlagKey} from {FeatureFlagsEnvironmentKey}.",
                    rawKey,
                    FeatureFlagsEnvironmentKey);
                continue;
            }

            bool? parsedValue = ParseFeatureFlagValue(rawValue);
            if (!parsedValue.HasValue)
            {
                this.logger.LogWarning(
                    "Ignoring invalid feature flag value for {FeatureFlagKey} from {FeatureFlagsEnvironmentKey}. Expected numeric values 1 or 0.",
                    rawKey,
                    FeatureFlagsEnvironmentKey);
                continue;
            }

            if (parsedFeatureFlags.ContainsKey(rawKey))
            {
                this.logger.LogWarning(
                    "Duplicate feature flag key {FeatureFlagKey} detected in {FeatureFlagsEnvironmentKey}; using the last value.",
                    rawKey,
                    FeatureFlagsEnvironmentKey);
            }

            parsedFeatureFlags[rawKey] = parsedValue.Value;
        }

        return parsedFeatureFlags;
    }

    private static bool? ParseFeatureFlagValue(string rawValue)
    {
        string normalizedValue = rawValue.Trim().ToLowerInvariant();
        if (normalizedValue == "1")
        {
            return true;
        }

        if (normalizedValue == "0")
        {
            return false;
        }

        return null;
    }
}
