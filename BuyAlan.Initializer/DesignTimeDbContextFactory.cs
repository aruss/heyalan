namespace BuyAlan.Initializer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using BuyAlan.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MainDataContext>
{
    public MainDataContext CreateDbContext(string[] args)
    {
        var dbBuilder = new DbContextOptionsBuilder<MainDataContext>();

        var connString = Environment.GetEnvironmentVariable("ConnectionStrings__buyalandb")
                     ?? "Host=localhost;Database=placeholder";

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
        dataSourceBuilder.EnableDynamicJson();
        NpgsqlDataSource dataSource = dataSourceBuilder.Build();

        dbBuilder.UseNpgsql(dataSource, npgsqlBuilder =>
            {
                // typeof(DesignTimeDbContextFactory).GetTypeInfo().Assembly.GetName().Name;
                npgsqlBuilder.MigrationsAssembly("BuyAlan.Initializer");  

                string prefix = Constants.TablePrefix.ToSnakeCase();
                if (!String.IsNullOrWhiteSpace(prefix)) {
                    prefix = $"{prefix}_";
                }
                
                npgsqlBuilder.MigrationsHistoryTable($"{prefix}migration_history");
            }
        );

        return new MainDataContext(dbBuilder.Options);
    }
}
