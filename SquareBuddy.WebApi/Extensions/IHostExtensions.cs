namespace SquareBuddy.WebApi;

using Microsoft.EntityFrameworkCore;
using SquareBuddy.Data;
using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId; 
        }

        return null; 
    }
}
public static class IHostExtensions
{
    // TODO: make it generic so it accepts any DbContext type
    public static void CheckIfAotSupported(this IHost app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using (var scope = app.Services.CreateScope())
        {
            Console.WriteLine("[AOT DEBUG] Starting Context Check...");
            try
            {
                var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<MainDataContext>>();

                // Check if the Model Extension is loaded in options
                var extension = options.FindExtension<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>();
                if (extension?.Model == null)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("[AOT FATAL] The Compiled Model is MISSING from DbContextOptions!");
                    Console.WriteLine("[AOT FATAL] EF Core will attempt dynamic generation and CRASH.");
                    Console.ResetColor();
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[AOT SUCCESS] Compiled Model Loaded: {extension.Model.GetType().Name}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AOT ERROR] Initialization failed: {ex.Message}");
            }
        }
    }
}