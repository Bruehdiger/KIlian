using KIlian.EfCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace KIlian.Features.Configuration.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSqlite(this IServiceCollection services) =>
        services.AddDbContextFactory<KIlianSqliteDbContext>((sp, builder) =>
        {
            Action<SqliteDbContextOptionsBuilder> sqliteOptions = options => options.MigrationsAssembly(typeof(KIlianSqliteDbContext).Assembly);
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionStringBuilder = new SqliteConnectionStringBuilder(config.GetConnectionString("KIlian") ?? "Data Source=");
            if (connectionStringBuilder.DataSource == ":memory:" || string.IsNullOrEmpty(connectionStringBuilder.DataSource)) //empty datasource = temp database
            {
                var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();
                builder.UseSqlite(connection, sqliteOptions);
            }
            else
            {
                builder.UseSqlite(connectionStringBuilder.ConnectionString, sqliteOptions);
            }
        });
}