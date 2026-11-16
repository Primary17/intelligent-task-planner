using System.Net.Http.Headers;
using TaskPlanner.Maui.Services;

namespace TaskPlanner.Maui.Api;

public sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthTokenStore _tokens;

    public AuthHeaderHandler(IAuthTokenStore tokens)
    {
        _tokens = tokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokens.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            request.Headers.Authorization = null;
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
