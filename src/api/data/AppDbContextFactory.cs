using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: true)
			.AddJsonFile($"appsettings.{environmentName}.json", optional: true)
			.AddUserSecrets(typeof(AppDbContextFactory).Assembly, optional: true)
			.AddEnvironmentVariables()
			.Build();

		var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
		var connectionString = configuration.GetConnectionString("DefaultConnection");
		optionsBuilder.UseSqlServer(connectionString);

		return new AppDbContext(optionsBuilder.Options);
	}
}


