namespace BuyAlan.Tests;

using BuyAlan.Data;
using BuyAlan.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

public class M36PrivacySmsSignupDataModelTests
{
    [Fact]
    public void MainDataContext_ModelContainsSmsConsentEntity()
    {
        using MainDataContext context = CreatePostgresContext();

        IEntityType? entityType = context.Model.FindEntityType(typeof(SmsConsent));

        Assert.NotNull(entityType);
    }

    [Fact]
    public void SmsConsent_ImplementsAuditAndIdContracts()
    {
        Assert.True(typeof(IEntityWithId).IsAssignableFrom(typeof(SmsConsent)));
        Assert.True(typeof(IEntityWithAudit).IsAssignableFrom(typeof(SmsConsent)));
    }

    [Fact]
    public void SmsConsent_HasExpectedRequiredProperties()
    {
        using MainDataContext context = CreatePostgresContext();
        IEntityType entityType = context.Model.FindEntityType(typeof(SmsConsent))!;

        IKey primaryKey = entityType.FindPrimaryKey()!;
        Assert.Single(primaryKey.Properties);
        Assert.Equal(nameof(SmsConsent.Id), primaryKey.Properties[0].Name);

        IProperty phoneNumberProperty = entityType.FindProperty(nameof(SmsConsent.PhoneNumber))!;
        IProperty consentSourceProperty = entityType.FindProperty(nameof(SmsConsent.ConsentSource))!;

        Assert.False(phoneNumberProperty.IsNullable);
        Assert.False(consentSourceProperty.IsNullable);
    }

    private static MainDataContext CreatePostgresContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=buyalan_tests;Username=test;Password=test")
            .Options;

        return new MainDataContext(options);
    }
}
