namespace TaskPlanner.Maui.Services;

/// <summary>
/// Базова URL API (без завершального слешу), наприклад http://10.0.2.2:5109/
/// </summary>
public sealed class AppSettings
{
    private const string Key = "api_base_url";

    /// <summary>Емулятор / VM → хост-машина.</summary>
    public const string DefaultEmulatorBaseUrl = "http://10.0.2.2:5109/";

    /// <summary>Windows / десктоп / пристрій у тій самій мережі, що й API.</summary>
    public const string DefaultLocalBaseUrl = "http://localhost:5109/";

    public string GetBaseUrl()
    {
        var s = Preferences.Default.Get(Key, DefaultLocalBaseUrl).Trim();
        if (!s.EndsWith('/'))
        {
            s += "/";
        }

        return s;
    }

    public void SetBaseUrl(string url)
    {
        var s = url.Trim();
        if (!s.EndsWith('/'))
        {
            s += "/";
        }

        Preferences.Default.Set(Key, s);
    }
}
