namespace TaskPlanner.Application.Security;

public static class AuthRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
}

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AuthenticatedUser = "AuthenticatedUser";
}
