using System.Net.Http;
using System.Text;
using System.Text.Json;
using Tvdb.Models;
using Tvdb.Paging;

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

        // If a paging wrapper is capturing, surface the pagination metadata it needs.
        if (TvdbResponseContext.Current is { } capture)
        {
            if (document.RootElement.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Object)
                capture.Links = links.Deserialize<Links>();
            if (document.RootElement.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String)
                capture.Status = status.GetString();
        }

        var original = response.Content;
        response.Content = new StringContent(data.GetRawText(), Encoding.UTF8, "application/json");
        original.Dispose();
        return response;
    }
}
