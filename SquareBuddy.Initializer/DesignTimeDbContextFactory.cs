namespace SquareBuddy.Initializer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using SquareBuddy.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MainDataContext>
{
    public MainDataContext CreateDbContext(string[] args)
    {
        var dbBuilder = new DbContextOptionsBuilder<MainDataContext>();

        var connString = Environment.GetEnvironmentVariable("ConnectionStrings__squarebuddydb")
                     ?? "Host=localhost;Database=placeholder";

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
        dataSourceBuilder.EnableDynamicJson();
        NpgsqlDataSource dataSource = dataSourceBuilder.Build();

        dbBuilder.UseNpgsql(dataSource, npgsqlBuilder =>
            {
                // typeof(DesignTimeDbContextFactory).GetTypeInfo().Assembly.GetName().Name;
                npgsqlBuilder.MigrationsAssembly("SquareBuddy.Initializer");  
                npgsqlBuilder.MigrationsHistoryTable($"{Constants.TablePrefix}_migration_history");
            }
        );

        return new MainDataContext(dbBuilder.Options);
    }
}
