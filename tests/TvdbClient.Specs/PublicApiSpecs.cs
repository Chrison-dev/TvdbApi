using System.Threading.Tasks;
using PublicApiGenerator;
using VerifyXunit;
using Xunit;

namespace TvdbClient.Specs;

/// <summary>
/// Snapshots each package's public API surface (via PublicApiGenerator + Verify). Any
/// change to the public surface fails until the .verified.txt is re-accepted — catching
/// accidental breaking changes, and forcing intentional ones to be reviewed.
/// </summary>
public class PublicApiSpecs
{
    [Fact]
    public Task TvdbClient_Models_public_api()
        => Verifier.Verify(typeof(Tvdb.Models.SeriesBaseRecord).Assembly.GeneratePublicApi());

    [Fact]
    public Task TvdbClient_Abstractions_public_api()
        => Verifier.Verify(typeof(Tvdb.Abstractions.ITvdbClient).Assembly.GeneratePublicApi());

    [Fact]
    public Task TvdbClient_public_api()
        => Verifier.Verify(typeof(Tvdb.Paging.TvdbPagingExtensions).Assembly.GeneratePublicApi());
}
