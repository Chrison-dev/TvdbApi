using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Tvdb.Clients;
using Tvdb.Handlers;
using Tvdb.Paging;
using Xunit;

namespace TvdbClient.Specs;

/// <summary>
/// End-to-end for the paginated list path: the envelope handler strips <c>data</c> and captures
/// <c>links</c>/<c>status</c>, the generated client deserializes the items, and
/// <c>GetPageAsync</c> folds them back into a <see cref="Tvdb.Abstractions.Page{T}"/>.
/// </summary>
public class PagingSpecs
{
    [Fact]
    public async Task GetPageAsync_returns_items_plus_pagination_metadata()
    {
        const string enveloped =
            """
            {"data":[{"id":1,"name":"A"},{"id":2,"name":"B"}],
             "status":"success",
             "links":{"prev":null,"self":"/series?page=0","next":"/series?page=1","total_items":100,"page_size":2}}
            """;

        using var http = new HttpClient(new EnvelopeUnwrappingHandler { InnerHandler = new StubHandler(enveloped) })
        {
            BaseAddress = new Uri("http://tvdb.test/"),
        };
        var series = new SeriesClient(http);

        var page = await series.GetPageAsync(page: 0);

        page.Items.Should().HaveCount(2);
        page.Items[0].Name.Should().Be("A");
        page.Status.Should().Be("success");
        page.HasNext.Should().BeTrue();
        page.HasPrevious.Should().BeFalse();
        page.TotalItems.Should().Be(100);
        page.PageSize.Should().Be(2);
    }

    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}
