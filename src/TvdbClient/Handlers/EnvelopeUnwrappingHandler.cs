using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Tvdb.Handlers;

/// <summary>
/// TheTVDB wraps every payload in a <c>{ data, status, links? }</c> envelope. The generated
/// clients are configured (via the codegen overlay) to expect the inner <c>data</c> shape
/// directly, so this handler peels the envelope off successful JSON responses before the client
/// deserializes them. Non-JSON, error, or already-unwrapped responses pass through untouched.
/// </summary>
internal sealed class EnvelopeUnwrappingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode ||
            response.Content.Headers.ContentType?.MediaType != "application/json")
        {
            return response;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
            return response;

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("data", out var data))
        {
            return response;
        }

        var original = response.Content;
        response.Content = new StringContent(data.GetRawText(), Encoding.UTF8, "application/json");
        original.Dispose();
        return response;
    }
}
