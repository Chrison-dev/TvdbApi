using System;
using System.Threading;
using Tvdb.Models;

namespace Tvdb.Paging;

/// <summary>
/// Ambient, async-flow-local capture of the pagination metadata (<c>links</c>/<c>status</c>)
/// that <see cref="Tvdb.Handlers.EnvelopeUnwrappingHandler"/> strips off list responses.
///
/// A paging wrapper opens a <see cref="Capture"/> scope <em>before</em> awaiting the generated
/// client, so the (mutable) capture object flows down the async call into the handler; the
/// handler fills it in; the wrapper reads it back after the await. This keeps the generated
/// clients returning plain collections while still surfacing the links as <c>Page&lt;T&gt;</c>.
/// </summary>
internal static class TvdbResponseContext
{
    private static readonly AsyncLocal<Capture?> Ambient = new();

    internal static Capture? Current => Ambient.Value;

    internal static Capture BeginCapture()
    {
        var capture = new Capture();
        Ambient.Value = capture;
        return capture;
    }

    internal sealed class Capture : IDisposable
    {
        public Links? Links { get; set; }
        public string? Status { get; set; }

        public void Dispose()
        {
            if (ReferenceEquals(Ambient.Value, this))
                Ambient.Value = null;
        }
    }
}
