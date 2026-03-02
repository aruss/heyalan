namespace ShelfBuddy.Tests;

using Microsoft.EntityFrameworkCore;
using ShelfBuddy.Data;
using ShelfBuddy.Data.Entities;
using ShelfBuddy.WebApi.Identity;

public class IdentityProvisioningTests
{
    [Fact]
    public async Task CreateInitialSubscriptionOwnerMembershipAsync_WhenUserHasNoMembership_CreatesOwnerMembershipAndSubscription()
    {
        MainDataContext dbContext = CreateContext();
        Guid userId = Guid.NewGuid();

        ApplicationUser user = new()
        {
            Id = userId,
            Email = "owner@example.com",
            UserName = "owner@example.com",
            DisplayName = "Owner"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        await IdentityEndpoints.CreateInitialSubscriptionOwnerMembershipAsync(dbContext, userId, CancellationToken.None);

        SubscriptionUser membership = await dbContext.SubscriptionUsers
            .SingleAsync(item => item.UserId == userId);

        Subscription subscription = await dbContext.Subscriptions
            .SingleAsync(item => item.Id == membership.SubscriptionId);

        Assert.Equal(SubscriptionUserRole.Owner, membership.Role);
        Assert.Equal(0, subscription.SubscriptionCreditBalance);
        Assert.Equal(0, subscription.TopUpCreditBalance);
    }

    [Fact]
    public async Task CreateInitialSubscriptionOwnerMembershipAsync_WhenUserAlreadyHasMembership_DoesNotCreateDuplicate()
    {
        MainDataContext dbContext = CreateContext();
        Guid userId = Guid.NewGuid();

        ApplicationUser user = new()
        {
            Id = userId,
            Email = "owner@example.com",
            UserName = "owner@example.com",
            DisplayName = "Owner"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        await IdentityEndpoints.CreateInitialSubscriptionOwnerMembershipAsync(dbContext, userId, CancellationToken.None);
        await IdentityEndpoints.CreateInitialSubscriptionOwnerMembershipAsync(dbContext, userId, CancellationToken.None);

        int membershipCount = await dbContext.SubscriptionUsers.CountAsync(item => item.UserId == userId);
        Assert.Equal(1, membershipCount);
    }

    [Fact]
    public void BuildLoginRedirectUrl_WithSubscriptionProvisionFailedCode_UsesExpectedAuthError()
    {
        string redirectUrl = IdentityEndpoints.BuildLoginRedirectUrl(
            "/admin?authError=stale",
            "subscription_provision_failed");

        Assert.Equal(
            "/login?returnUrl=%2Fadmin&authError=subscription_provision_failed",
            redirectUrl);
    }

    private static MainDataContext CreateContext()
    {
        DbContextOptions<MainDataContext> options = new DbContextOptionsBuilder<MainDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MainDataContext(options);
    }
}
