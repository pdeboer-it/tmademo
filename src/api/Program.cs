using System.Text.Json;
using api.Data;
using api.Models;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddCors(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);

var app = builder.Build();

app.MapControllers();

app.UseHttpsRedirection();
app.UseCors("SpaCors");
app.UseAuthentication();
app.UseAuthorization();
app.ApplyPendingMigrations();

app.Run();
