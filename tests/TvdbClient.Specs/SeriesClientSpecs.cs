using System.Threading.Tasks;
using FluentAssertions;
using Mockly;
using Tvdb.Clients;
using Xunit;

namespace TvdbClient.Specs;

/// <summary>
/// The generated clients deserialize the (already-unwrapped) body directly into the DTO.
/// Uses Mockly to stand in for TheTVDB's HTTP endpoint.
/// </summary>
public class SeriesClientSpecs
{
    [Fact]
    public async Task Deserializes_a_series_record_from_the_response_body()
    {
        var mock = new HttpMock();
        mock.ForGet()
            .WithPath("/series/121361")
            .RespondsWithJsonContent(new { id = 121361, name = "Doctor Who" });

        var series = new SeriesClient(mock.GetClient());

        var record = await series.SeriesGetAsync(121361);

        record.Id.Should().Be(121361);
        record.Name.Should().Be("Doctor Who");
    }
}
