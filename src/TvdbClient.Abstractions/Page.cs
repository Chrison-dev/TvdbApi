using System.Collections.Generic;
using Tvdb.Models;

namespace Tvdb.Abstractions;

/// <summary>
/// A page of results from a paginated TheTVDB list endpoint: the items plus the
/// pagination <see cref="Links"/> and status peeled from the response envelope.
/// </summary>
public sealed class Page<T>
{
    public Page(IReadOnlyList<T> items, Links? links, string? status)
    {
        Items = items;
        Links = links;
        Status = status;
    }

    /// <summary>The items on this page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Pagination links (prev/self/next) and totals, or <c>null</c> if absent.</summary>
    public Links? Links { get; }

    /// <summary>The envelope status (e.g. <c>"success"</c>).</summary>
    public string? Status { get; }

    /// <summary>True if the API reported a next page.</summary>
    public bool HasNext => !string.IsNullOrWhiteSpace(Links?.Next);

    /// <summary>True if the API reported a previous page.</summary>
    public bool HasPrevious => !string.IsNullOrWhiteSpace(Links?.Prev);

    /// <summary>Total item count across all pages, when the API provides it.</summary>
    public int? TotalItems => Links?.Total_items;

    /// <summary>Page size, when the API provides it.</summary>
    public int? PageSize => Links?.Page_size;
}
