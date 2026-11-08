namespace TaskPlanner.Maui.Services;

/// <summary>
/// Підставляє актуальну базову URL з <see cref="AppSettings"/> перед кожним запитом
/// (щоб зміна URL у налаштуваннях не вимагала перезапуску процесу).
/// </summary>
public sealed class DynamicBaseAddressHandler : DelegatingHandler
{
    private readonly AppSettings _settings;

    public DynamicBaseAddressHandler(AppSettings settings)
    {
        _settings = settings;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var baseUri = new Uri(_settings.GetBaseUrl(), UriKind.Absolute);
        var requestUri = request.RequestUri;

        if (requestUri is null || !requestUri.IsAbsoluteUri)
        {
            request.RequestUri = requestUri is null ? baseUri : new Uri(baseUri, requestUri);
        }
        else
        {
            // HttpClient уже склеїв відносний шлях зі стартовим BaseAddress.
            // Залишаємо шлях і запит, а origin беремо з актуальних налаштувань.
            request.RequestUri = new Uri(baseUri, requestUri.PathAndQuery.TrimStart('/'));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
