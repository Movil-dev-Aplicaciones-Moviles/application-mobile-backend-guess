using BackendAwSmartstay.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackendAwSmartstay.API.shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

/// <summary>
/// Provides extension methods for <see cref="WebApplicationBuilder"/> to configure persistence-related services.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    public static void AddDatabaseConfigurationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = BuildConnectionString(builder.Configuration);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found or is empty.");
            }

            if (connectionString.Contains('%'))
            {
                throw new InvalidOperationException(
                    "The database connection string still contains unresolved placeholders. " +
                    "Configure DATABASE_HOST, DATABASE_PORT, DATABASE_NAME, DATABASE_USER and DATABASE_PASSWORD in Render.");
            }

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

    private static string BuildConnectionString(IConfiguration configuration)
    {
        var host = configuration["DATABASE_HOST"]
                   ?? configuration["DATABASE_URL"];
        var port = configuration["DATABASE_PORT"];
        var database = configuration["DATABASE_NAME"]
                       ?? configuration["DATABASE_SCHEMA"];
        var user = configuration["DATABASE_USER"];
        var password = configuration["DATABASE_PASSWORD"];

        if (!string.IsNullOrWhiteSpace(host)
            && !string.IsNullOrWhiteSpace(port)
            && !string.IsNullOrWhiteSpace(database)
            && !string.IsNullOrWhiteSpace(user)
            && password is not null)
        {
            if (!int.TryParse(port, out _))
            {
                throw new InvalidOperationException($"Invalid DATABASE_PORT value '{port}'. Use a numeric port like 3306.");
            }

            return $"server={host};port={port};user={user};password={password};database={database}";
        }

        return configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }
}
