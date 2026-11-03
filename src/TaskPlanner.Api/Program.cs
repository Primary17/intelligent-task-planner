using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Api;
using TaskPlanner.Api.Infrastructure;
using TaskPlanner.Application.Validation;
using TaskPlanner.Infrastructure;
using TaskPlanner.Infrastructure.Persistence;
using TaskPlanner.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
var demoMode = args.Contains("--demo", StringComparer.OrdinalIgnoreCase);

if (demoMode)
{
    builder.Logging.SetMinimumLevel(LogLevel.Error);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddApiSecurity(builder.Configuration);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateStudyTaskRequestValidator>();
builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>())
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest
            };
            return new BadRequestObjectResult(problem);
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddApiDocumentation();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContextBase>();
    await db.Database.MigrateAsync();
    await AuthDataSeeder.SeedAsync(app.Services, builder.Configuration, app.Environment);

    if (args.Contains("--demo", StringComparer.OrdinalIgnoreCase))
    {
        var scenario = scope.ServiceProvider.GetRequiredService<Lab1ScenarioService>();
        var report = await scenario.RunAsync();
        Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        }));
        return;
    }
}

app.UseGlobalExceptionHandling(app.Environment);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();
app.UseCors(AuthConfiguration.CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
