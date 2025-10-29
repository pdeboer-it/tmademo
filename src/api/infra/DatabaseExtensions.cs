using api.Data;
using Microsoft.EntityFrameworkCore;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        return services;
    }

    public static void ApplyPendingMigrations(this WebApplication app)
    {
        // Apply pending EF Core migrations at startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope
                .ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Startup");
            try
            {
                db.Database.Migrate();
                logger.LogInformation("Database migration completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database migration failed at startup.");
            }
        }
    }
}
