using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Tvdb.Handlers;
using Xunit;

namespace TvdbClient.Specs;

/// <summary>
/// The generated clients expect the inner `data` shape (the codegen overlay strips the
/// envelope), so <see cref="EnvelopeUnwrappingHandler"/> must peel `{ data, status }` off
/// successful JSON responses — and leave everything else alone.
/// </summary>
public class EnvelopeUnwrappingSpecs
{
    static HttpClient ClientReturning(HttpStatusCode status, string body, string mediaType = "application/json")
        => new(new EnvelopeUnwrappingHandler { InnerHandler = new StubHandler(status, body, mediaType) });

    [Fact]
    public async Task Unwraps_the_data_envelope_on_success()
    {
        var http = ClientReturning(HttpStatusCode.OK, """{"data":{"id":42,"name":"X"},"status":"success"}""");

        var body = await (await http.GetAsync("http://tvdb.test/series/42")).Content.ReadAsStringAsync();

        body.Should().Be("""{"id":42,"name":"X"}""");
    }

    [Fact]
    public async Task Leaves_a_body_without_a_data_property_untouched()
    {
        var http = ClientReturning(HttpStatusCode.OK, """{"token":"abc"}""");

        var body = await (await http.GetAsync("http://tvdb.test/whatever")).Content.ReadAsStringAsync();

        body.Should().Be("""{"token":"abc"}""");
    }

    [Fact]
    public async Task Leaves_error_responses_untouched()
    {
        var http = ClientReturning(HttpStatusCode.NotFound, """{"data":{"id":1},"status":"failure"}""");

        var response = await http.GetAsync("http://tvdb.test/series/1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"data\"");
    }

    [Fact]
    public async Task Leaves_non_json_responses_untouched()
    {
        var http = ClientReturning(HttpStatusCode.OK, "plain text", mediaType: "text/plain");

        var body = await (await http.GetAsync("http://tvdb.test/thing")).Content.ReadAsStringAsync();

        body.Should().Be("plain text");
    }

    private sealed class StubHandler(HttpStatusCode status, string body, string mediaType) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, mediaType),
            });
    }
}
