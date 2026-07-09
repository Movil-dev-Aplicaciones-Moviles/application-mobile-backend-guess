using BackendAwSmartstay.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackendAwSmartstay.API.shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

/// <summary>
/// Provides extension methods for <see cref="WebApplicationBuilder"/> to configure persistence-related services.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    public static string GetMySqlConnectionString(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        // 1) Preferred option for Render: set the complete connection string as:
        // ConnectionStrings__DefaultConnection = Server=...;Port=3306;Database=...;User=...;Password=...;SslMode=Preferred;AllowPublicKeyRetrieval=True;
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(connectionString)
            && !connectionString.Contains('%')
            && !connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        // 2) Support providers that expose a full URL, for example:
        // mysql://user:password@host:3306/database
        var databaseUrl = configuration["DATABASE_URL"] ?? configuration["MYSQL_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl)
            && Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
            && uri.Scheme.StartsWith("mysql", StringComparison.OrdinalIgnoreCase))
        {
            var userInfo = uri.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty);
            var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty);
            var database = uri.AbsolutePath.Trim('/');
            var port = uri.Port > 0 ? uri.Port : 3306;

            return $"Server={uri.Host};Port={port};Database={database};User={user};Password={password};SslMode=Preferred;AllowPublicKeyRetrieval=True;";
        }

        // 3) Support separated environment variables.
        var host = configuration["DATABASE_HOST"]
                   ?? configuration["DATABASE_URL"]
                   ?? configuration["MYSQLHOST"];
        var portValue = configuration["DATABASE_PORT"]
                        ?? configuration["MYSQLPORT"]
                        ?? "3306";
        var databaseName = configuration["DATABASE_NAME"]
                           ?? configuration["DATABASE_SCHEMA"]
                           ?? configuration["MYSQLDATABASE"];
        var user = configuration["DATABASE_USER"]
                   ?? configuration["MYSQLUSER"];
        var passwordValue = configuration["DATABASE_PASSWORD"]
                            ?? configuration["MYSQLPASSWORD"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(databaseName)
            || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(passwordValue))
        {
            throw new InvalidOperationException(
                "MySQL is not configured. Set ConnectionStrings__DefaultConnection or DATABASE_HOST, DATABASE_PORT, DATABASE_NAME, DATABASE_USER and DATABASE_PASSWORD in Render.");
        }

        if (!uint.TryParse(portValue, out var port))
        {
            throw new InvalidOperationException($"Invalid DATABASE_PORT value: {portValue}. Use a number, for example 3306.");
        }

        return $"Server={host};Port={port};Database={databaseName};User={user};Password={passwordValue};SslMode=Preferred;AllowPublicKeyRetrieval=True;";
    }

    public static void AddDatabaseConfigurationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = builder.GetMySqlConnectionString();

            if (builder.Environment.IsDevelopment())
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                    .LogTo(Console.WriteLine, LogLevel.Information)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
            else
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                    .LogTo(Console.WriteLine, LogLevel.Error)
                    .EnableDetailedErrors();
            }
        });
    }
}
