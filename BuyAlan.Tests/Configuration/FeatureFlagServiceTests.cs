namespace BuyAlan.Tests;

using BuyAlan.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class FeatureFlagServiceTests
{
    [Theory]
    [InlineData("landingPricing=1", FeatureFlagNames.LandingPricing, true)]
    [InlineData("landingPricing=0", FeatureFlagNames.LandingPricing, false)]
    [InlineData("teamMembers=1", FeatureFlagNames.TeamMembers, true)]
    [InlineData("teamMembers=0", FeatureFlagNames.TeamMembers, false)]
    public void IsFeatureEnabled_WhenConfiguredWithNumericValue_ReturnsParsedResult(
        string featureFlags,
        string featureName,
        bool expectedValue)
    {
        FeatureFlagService service = CreateService(featureFlags);

        bool isEnabled = service.IsFeatureEnabled(featureName);

        Assert.Equal(expectedValue, isEnabled);
    }

    [Fact]
    public void IsFeatureEnabled_WhenWhitespaceAndTrailingSeparatorsExist_TrimsAndParses()
    {
        FeatureFlagService service = CreateService(" landingPricing = 1 ; teamMembers = 0 ; ");

        Assert.True(service.IsFeatureEnabled(FeatureFlagNames.LandingPricing));
        Assert.False(service.IsFeatureEnabled(FeatureFlagNames.TeamMembers));
    }

    [Fact]
    public void IsFeatureEnabled_WhenKnownFlagIsMissing_FallsBackToKnownDefault()
    {
        FeatureFlagService service = CreateService("landingPricing=1");

        Assert.True(service.IsFeatureEnabled(FeatureFlagNames.LandingPricing));
        Assert.True(service.IsFeatureEnabled(FeatureFlagNames.TeamMembers));
    }

    [Fact]
    public void IsFeatureEnabled_WhenFeatureFlagsAreMissing_ReturnsKnownDefaults()
    {
        FeatureFlagService service = CreateService(null);

        Assert.False(service.IsFeatureEnabled(FeatureFlagNames.LandingPricing));
        Assert.True(service.IsFeatureEnabled(FeatureFlagNames.TeamMembers));
    }

    [Theory]
    [InlineData("landingPricing=true", FeatureFlagNames.LandingPricing, false)]
    [InlineData("teamMembers=yes", FeatureFlagNames.TeamMembers, true)]
    public void IsFeatureEnabled_WhenValueIsInvalid_LogsWarningAndFallsBackToDefault(
        string featureFlags,
        string featureName,
        bool expectedValue)
    {
        RecordingLogger<FeatureFlagService> logger = new();
        FeatureFlagService service = CreateService(featureFlags, logger);

        bool isEnabled = service.IsFeatureEnabled(featureName);

        Assert.Equal(expectedValue, isEnabled);
        Assert.Contains(logger.Entries, entry => entry.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public void IsFeatureEnabled_WhenSegmentsAreMalformed_LogsWarningAndIgnoresThem()
    {
        RecordingLogger<FeatureFlagService> logger = new();
        FeatureFlagService service = CreateService("landingPricing=1=0;badSegment", logger);

        bool isLandingPricingEnabled = service.IsFeatureEnabled(FeatureFlagNames.LandingPricing);
        bool isTeamMembersEnabled = service.IsFeatureEnabled(FeatureFlagNames.TeamMembers);

        Assert.False(isLandingPricingEnabled);
        Assert.True(isTeamMembersEnabled);
        Assert.Equal(2, logger.Entries.Count(entry => entry.LogLevel == LogLevel.Warning));
    }

    [Fact]
    public void IsFeatureEnabled_WhenDuplicateKeysExist_UsesLastValueAndLogsWarning()
    {
        RecordingLogger<FeatureFlagService> logger = new();
        FeatureFlagService service = CreateService("teamMembers=1;teamMembers=0", logger);

        bool isEnabled = service.IsFeatureEnabled(FeatureFlagNames.TeamMembers);

        Assert.False(isEnabled);
        Assert.Contains(
            logger.Entries,
            entry => entry.LogLevel == LogLevel.Warning &&
                     entry.Message.Contains("Duplicate feature flag key", StringComparison.Ordinal));
    }

    [Fact]
    public void IsFeatureEnabled_WhenUnknownConfiguredKeyExists_IgnoresItWithoutBreakingKnownFlags()
    {
        RecordingLogger<FeatureFlagService> logger = new();
        FeatureFlagService service = CreateService("doesNotExist=1;landingPricing=1", logger);

        bool isLandingPricingEnabled = service.IsFeatureEnabled(FeatureFlagNames.LandingPricing);
        bool isUnknownEnabled = service.IsFeatureEnabled("doesNotExist");

        Assert.True(isLandingPricingEnabled);
        Assert.False(isUnknownEnabled);
        Assert.Contains(
            logger.Entries,
            entry => entry.LogLevel == LogLevel.Warning &&
                     entry.Message.Contains("Ignoring unknown feature flag key", StringComparison.Ordinal));
    }

    [Fact]
    public void IsFeatureEnabled_WhenFeatureNameIsUnknown_ReturnsFalse()
    {
        FeatureFlagService service = CreateService("landingPricing=1");

        bool isEnabled = service.IsFeatureEnabled("doesNotExist");

        Assert.False(isEnabled);
    }

    [Fact]
    public void IsFeatureEnabled_WhenFeatureNameHasWhitespace_UsesTrimmedValue()
    {
        FeatureFlagService service = CreateService("landingPricing=1");

        bool isEnabled = service.IsFeatureEnabled(" landingPricing ");

        Assert.True(isEnabled);
    }

    private static FeatureFlagService CreateService(
        string? featureFlags,
        ILogger<FeatureFlagService>? logger = null)
    {
        Dictionary<string, string?> values = new();
        if (featureFlags is not null)
        {
            values["FEATURE_FLAGS"] = featureFlags;
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new FeatureFlagService(
            configuration,
            logger ?? new RecordingLogger<FeatureFlagService>());
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            this.Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }

        public sealed record LogEntry(LogLevel LogLevel, string Message);

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
