namespace ShelfBuddy.WebApi.Infrastructure;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ShelfBuddy.Data;

public static class EntityFrameworkBuilderExtensions
{
    public static TBuilder AddDatabaseServices<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddDbContext<MainDataContext>(options =>
        {
            var connStr = builder.Configuration.GetConnectionString("shelfbuddy");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new InvalidOperationException("DB Connection Missing");
            }

            options.UseNpgsql(connStr);
        });

        builder.Services.AddDataProtection()
            .PersistKeysToDbContext<MainDataContext>();


        return builder; 
    }
}
