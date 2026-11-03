using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TaskPlanner.Application.Security;
using TaskPlanner.Infrastructure.Security;

namespace TaskPlanner.Api;

public static class AuthConfiguration
{
    public const string CorsPolicyName = "TaskPlannerCors";

    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(jwtSection);

        var secret = configuration["Jwt:Secret"]
                     ?? configuration["JWT_SECRET"]
                     ?? throw new InvalidOperationException(
                         "Configure JWT secret via Jwt:Secret or JWT_SECRET environment variable.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"] ?? "TaskPlanner",
                    ValidAudience = jwtSection["Audience"] ?? "TaskPlannerClients",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var endpoint = context.HttpContext.GetEndpoint();
                        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
                        {
                            context.NoResult();
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";
                        return context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Title = "Unauthorized",
                            Detail = "Authentication is required.",
                            Status = StatusCodes.Status401Unauthorized
                        });
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/problem+json";
                        return context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Title = "Forbidden",
                            Detail = "You do not have permission to access this resource.",
                            Status = StatusCodes.Status403Forbidden
                        });
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.AdminOnly, policy =>
                policy.RequireRole(AuthRoles.Admin));

            options.AddPolicy(AuthPolicies.AuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());
        });

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                var origins = configuration.GetSection("Cors:Origins").Get<string[]>()
                              ?? ["http://localhost:5173", "http://localhost:3000"];

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
