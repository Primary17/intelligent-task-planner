namespace TaskPlanner.Maui.Services;

/// <summary>
/// Зберігає JWT для подальших запитів до API.
/// </summary>
public interface IAuthTokenStore
{
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);

    Task SetTokenAsync(string token, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
