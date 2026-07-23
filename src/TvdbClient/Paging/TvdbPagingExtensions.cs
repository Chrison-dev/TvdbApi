using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tvdb.Abstractions;
using Tvdb.Clients;
using Tvdb.Models;

namespace Tvdb.Paging;

/// <summary>
/// <c>GetPageAsync</c> overloads for the eight paginated TheTVDB list endpoints. Each calls the
/// generated client (which returns the item collection) inside a <see cref="TvdbResponseContext"/>
/// capture scope, so the pagination <c>links</c>/<c>status</c> the envelope handler strips are
/// folded back into a <see cref="Page{T}"/>.
/// </summary>
public static class TvdbPagingExtensions
{
    public static Task<Page<SeriesBaseRecord>> GetPageAsync(this ISeriesClient client, long? page = null, CancellationToken cancellationToken = default)
        => Paged(() => client.SeriesGetAsync(page, cancellationToken));

    public static Task<Page<MovieBaseRecord>> GetPageAsync(this IMoviesClient client, long? page = null, CancellationToken cancellationToken = default)
        => Paged(() => client.MoviesGetAsync(page, cancellationToken));

    public static Task<Page<EpisodeBaseRecord>> GetPageAsync(this IEpisodesClient client, long? page = null, CancellationToken cancellationToken = default)
        => Paged(() => client.EpisodesGetAsync(page, cancellationToken));

    public static Task<Page<PeopleBaseRecord>> GetPageAsync(this IPeopleClient client, long? page = null, CancellationToken cancellationToken = default)
        => Paged(() => client.PeopleGetAsync(page, cancellationToken));

    public static Task<Page<ListBaseRecord>> GetPageAsync(this IListsClient client, long? page = null, CancellationToken cancellationToken = default)
        => Paged(() => client.ListsGetAsync(page, cancellationToken));

    public static Task<Page<SeasonBaseRecord>> GetPageAsync(this ISeasonsClient client, long? page = null, CancellationToken cancellationToken = default)
        => Paged(() => client.SeasonsGetAsync(page, cancellationToken));

    public static Task<Page<Company>> GetPageAsync(this ICompaniesClient client, long? page = null, CancellationToken cancellationToken = default)
        => Paged(() => client.CompaniesGetAsync(page, cancellationToken));

    public static Task<Page<EntityUpdate>> GetPageAsync(this IUpdatesClient client, long since, UpdatesType? type = null, UpdatesAction? action = null, long? page = null, CancellationToken cancellationToken = default)
        => Paged(() => client.UpdatesAsync(since, type, action, page, cancellationToken));

    private static async Task<Page<T>> Paged<T>(Func<Task<ICollection<T>>> call)
    {
        using var capture = TvdbResponseContext.BeginCapture();
        var items = await call().ConfigureAwait(false);
        var list = items is null ? new List<T>() : new List<T>(items);
        return new Page<T>(list, capture.Links, capture.Status);
    }
}
