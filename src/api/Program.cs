using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// CORS for SPA
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
	options.AddPolicy("SpaCors", policy =>
	{
		policy.WithOrigins(allowedOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});

// JWT Bearer authentication (Microsoft Entra ID)
var authority = builder.Configuration["Jwt:Authority"];
var audience = builder.Configuration["Jwt:Audience"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.Authority = authority;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidAudience = audience
		};
	});
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("SpaCors");
app.UseAuthentication();
app.UseAuthorization();


// Apply pending EF Core migrations at startup
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
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

// Simple DB health check
app.MapGet("/health/db", async (AppDbContext context) =>
{
	try
	{
		var canConnect = await context.Database.CanConnectAsync();
		var pending = await context.Database.GetPendingMigrationsAsync();
		return Results.Ok(new
		{
			connected = canConnect,
			pendingMigrations = pending
		});
	}
	catch (Exception ex)
	{
		return Results.Problem(ex.Message);
	}
})
.WithName("DbHealth")
.WithOpenApi();


app.MapGet("/candidates", async (AppDbContext context) =>
{
    return await context.Candidates.ToListAsync();
})
.WithName("GetCandidates")
.RequireAuthorization()
.WithOpenApi();

app.MapGet("/candidates/{id}", async (AppDbContext context, int id) =>
{
    return await context.Candidates.FindAsync(id);
})
.WithName("GetCandidate")
.RequireAuthorization()
.WithOpenApi();

app.MapPost("/candidates", async (AppDbContext context, Candidate candidate) =>
{
    await context.Candidates.AddAsync(candidate);
    await context.SaveChangesAsync();
    return candidate;
})
.WithName("CreateCandidate")
.RequireAuthorization()
.WithOpenApi();

app.Run();

