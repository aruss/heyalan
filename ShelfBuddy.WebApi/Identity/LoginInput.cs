namespace ShelfBuddy.WebApi.Identity;

// Copied from  Microsoft.AspNetCore.Identity.Data.LoginRequest
/// <summary>
/// The input type for login endpoint.
/// </summary>
/// <param name="Email">The user's email address which acts as a user name.</param>
/// <param name="Password">The user's password.</param>
public record LoginInput(
    string Email,
    string Password
);
