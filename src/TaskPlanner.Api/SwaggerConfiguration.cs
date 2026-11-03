using Microsoft.OpenApi.Models;

namespace TaskPlanner.Api;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Task Planner API",
                Version = "v1",
                Description = "STEM task planner API with JWT authentication (MPP lab 3)."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });

            options.OperationFilter<BearerSecurityOperationFilter>();
        });

        return services;
    }
}
