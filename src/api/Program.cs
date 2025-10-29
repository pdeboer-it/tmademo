using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

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
var apiClientId = audience?.StartsWith("api://", StringComparison.OrdinalIgnoreCase) == true
	? audience.Substring("api://".Length)
	: audience;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.Authority = authority;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidAudiences = new[] { audience, apiClientId }
		};
	});
builder.Services.AddAuthorization();

// Distributed cache (Redis if configured, else in-memory)
var redisConnection = builder.Configuration.GetConnectionString("Redis")
	?? builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
	builder.Services.AddStackExchangeRedisCache(options =>
	{
		options.Configuration = redisConnection;
	});
}
else
{
	builder.Services.AddDistributedMemoryCache();
}

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


app.MapGet("/candidates", async (AppDbContext context, IDistributedCache cache) =>
{
    const string cacheKey = "candidates_all";
    var cached = await cache.GetStringAsync(cacheKey);
    if (cached != null)
    {
    	var fromCache = JsonSerializer.Deserialize<List<Candidate>>(cached);
    	return Results.Ok(fromCache);
    }

    var list = await context.Candidates.ToListAsync();
    await cache.SetStringAsync(
    	cacheKey,
    	JsonSerializer.Serialize(list),
    	new DistributedCacheEntryOptions
    	{
    		AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    	});
    return Results.Ok(list);
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

app.MapPost("/candidates", async (AppDbContext context, Candidate candidate, IDistributedCache cache) =>
{
    await context.Candidates.AddAsync(candidate);
    await context.SaveChangesAsync();
    await cache.RemoveAsync("candidates_all");
    return candidate;
})
.WithName("CreateCandidate")
.RequireAuthorization()
.WithOpenApi();

app.MapGet("/search", async (IConfiguration cfg, string q) =>
{
    var endpoint = new Uri(cfg["Search:Endpoint"]!);
    var indexName = cfg["Search:IndexName"]!;
    var key = new AzureKeyCredential(cfg["Search:ApiKey"]!);

    var client = new SearchClient(endpoint, indexName, key);
    var options = new SearchOptions { Size = 10 };
    var results = await client.SearchAsync<SearchDocument>(string.IsNullOrWhiteSpace(q) ? "*" : q, options);

    var docs = new List<object>();
    await foreach (var r in results.Value.GetResultsAsync())
        docs.Add(r.Document);

    return Results.Ok(docs);
})
.WithName("Search")
.RequireAuthorization()
.WithOpenApi();

app.Run();

