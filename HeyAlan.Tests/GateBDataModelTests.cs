namespace HeyAlan.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using HeyAlan.Data;
using HeyAlan.Data.Entities;

public class GateBDataModelTests
{
    [Fact]
    public void MainDataContext_ModelContainsGateBEntities()
    {
        using MainDataContext context = CreateContext();

        IEntityType? connectionEntity = context.Model.FindEntityType(typeof(SubscriptionSquareConnection));
        IEntityType? onboardingEntity = context.Model.FindEntityType(typeof(SubscriptionOnboardingState));

        Assert.NotNull(connectionEntity);
        Assert.NotNull(onboardingEntity);
    }

    [Fact]
    public void SubscriptionSquareConnection_HasExpectedIndexesAndRelationships()
    {
        using MainDataContext context = CreateContext();
        IEntityType entityType = context.Model.FindEntityType(typeof(SubscriptionSquareConnection))!;

        IKey primaryKey = entityType.FindPrimaryKey()!;
        Assert.Single(primaryKey.Properties);
        Assert.Equal(nameof(SubscriptionSquareConnection.SubscriptionId), primaryKey.Properties[0].Name);

        IIndex? subscriptionIndex = entityType.GetIndexes()
            .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(SubscriptionSquareConnection.SubscriptionId)]));
        Assert.NotNull(subscriptionIndex);
        Assert.True(subscriptionIndex!.IsUnique);

        IIndex? merchantIndex = entityType.GetIndexes()
            .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(SubscriptionSquareConnection.SquareMerchantId)]));
        Assert.NotNull(merchantIndex);

        IForeignKey subscriptionForeignKey = entityType.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Subscription));
        Assert.Equal(DeleteBehavior.Cascade, subscriptionForeignKey.DeleteBehavior);

        IForeignKey connectedByUserForeignKey = entityType.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(ApplicationUser));
        Assert.Equal(DeleteBehavior.Restrict, connectedByUserForeignKey.DeleteBehavior);
    }

    [Fact]
    public void SubscriptionOnboardingState_HasExpectedIndexesAndRelationships()
    {
        using MainDataContext context = CreateContext();
        IEntityType entityType = context.Model.FindEntityType(typeof(SubscriptionOnboardingState))!;

        IKey primaryKey = entityType.FindPrimaryKey()!;
        Assert.Single(primaryKey.Properties);
        Assert.Equal(nameof(SubscriptionOnboardingState.SubscriptionId), primaryKey.Properties[0].Name);

        IIndex? subscriptionIndex = entityType.GetIndexes()
            .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(SubscriptionOnboardingState.SubscriptionId)]));
        Assert.NotNull(subscriptionIndex);
        Assert.True(subscriptionIndex!.IsUnique);

        IForeignKey subscriptionForeignKey = entityType.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Subscription));
        Assert.Equal(DeleteBehavior.Cascade, subscriptionForeignKey.DeleteBehavior);

        IForeignKey primaryAgentForeignKey = entityType.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Agent));
        Assert.Equal(DeleteBehavior.SetNull, primaryAgentForeignKey.DeleteBehavior);
    }

    [Fact]
    public void Agent_GateBFields_AreMappedAsNullable()
    {
        using MainDataContext context = CreateContext();
        IEntityType entityType = context.Model.FindEntityType(typeof(Agent))!;

        IProperty personalityProperty = entityType.FindProperty(nameof(Agent.Personality))!;
        IProperty whatsappNumberProperty = entityType.FindProperty(nameof(Agent.WhatsappNumber))!;
        IProperty twilioPhoneNumberProperty = entityType.FindProperty(nameof(Agent.TwilioPhoneNumber))!;
        IProperty telegramBotTokenProperty = entityType.FindProperty(nameof(Agent.TelegramBotToken))!;

        Assert.True(personalityProperty.IsNullable);
        Assert.True(whatsappNumberProperty.IsNullable);
        Assert.True(twilioPhoneNumberProperty.IsNullable);
        Assert.True(telegramBotTokenProperty.IsNullable);
    }

    [Fact]
    public void Agent_TelegramBotTokenIndex_IsUniqueWithFilter()
    {
        using MainDataContext context = CreateContext();
        IEntityType entityType = context.Model.FindEntityType(typeof(Agent))!;

        IIndex? telegramTokenIndex = entityType.GetIndexes()
            .SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(Agent.TelegramBotToken)]));

        Assert.NotNull(telegramTokenIndex);
        Assert.True(telegramTokenIndex!.IsUnique);
        Assert.Equal("\"TelegramBotToken\" IS NOT NULL", telegramTokenIndex.GetFilter());
    }

    [Fact]
    public void GateBEntities_ImplementAuditContract()
    {
        Assert.True(typeof(IEntityWithAudit).IsAssignableFrom(typeof(SubscriptionSquareConnection)));
        Assert.True(typeof(IEntityWithAudit).IsAssignableFrom(typeof(SubscriptionOnboardingState)));
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=heyalan_tests;Username=test;Password=test")
            .Options;

        return new MainDataContext(options);
    }
}
