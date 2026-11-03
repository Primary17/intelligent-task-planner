namespace TaskPlanner.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TaskPlanner";
    public string Audience { get; set; } = "TaskPlannerClients";
    public int AccessTokenMinutes { get; set; } = 60;
}
