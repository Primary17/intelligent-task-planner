namespace TaskPlanner.Maui.Services;

public sealed class AuthTokenStore : IAuthTokenStore
{
    private const string Key = "taskplanner_jwt";

    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default) =>
        SecureStorage.Default.GetAsync(Key);

    public Task SetTokenAsync(string token, CancellationToken cancellationToken = default) =>
        SecureStorage.Default.SetAsync(Key, token);

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        SecureStorage.Default.Remove(Key);
        await Task.CompletedTask;
    }
}
