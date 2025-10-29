using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // JWT Bearer authentication (Microsoft Entra ID)
        var authority = configuration["Jwt:Authority"];
        var audience = configuration["Jwt:Audience"];
        var apiClientId =
            audience?.StartsWith("api://", StringComparison.OrdinalIgnoreCase) == true
                ? audience.Substring("api://".Length)
                : audience;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidAudiences = new[] { audience, apiClientId },
                };
            });

        services.AddAuthorization();

        return services;
    }
}
