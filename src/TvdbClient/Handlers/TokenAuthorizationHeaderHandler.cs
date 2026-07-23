using Tvdb.Abstractions;

namespace Tvdb.Handlers;

/// <summary>
/// Handler to add Authorization Header to the request
/// </summary>
/// <param name="tokenProvider"></param>
public class TokenAuthorizationHeaderHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    #region Overrides
    /// <summary>
    /// Send the request with the Authorization Header
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Acquire the token
        var token = await tokenProvider.AcquireTokenAsync(cancellationToken);

        // Add the Authorization Header to the request
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Token.TokenType, token.AccessToken);
        return await base.SendAsync(request, cancellationToken);
    }
    #endregion
}
