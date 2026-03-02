namespace ShelfBuddy.WebApi.Identity;

/// <summary>
/// Represents the currently authenticated user.
/// </summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="Roles">The roles assigned to the user.</param>
/// <param name="ActiveSubscriptionId">The selected active subscription for session-scoped operations.</param>
public record CurrentUserResult(Guid Id, string Email, string DisplayName, string[] Roles, Guid? ActiveSubscriptionId);
