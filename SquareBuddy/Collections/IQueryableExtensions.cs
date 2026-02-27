namespace SquareBuddy.Collections;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

/// <summary>
/// Extension methods for applying paging and cursor-based pagination to IQueryable sources.
/// </summary>
public static class IQueryableExtensions
{ 
    /// <summary>
  /// Materializes a page of items and returns paging metadata.
  /// </summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var items = await source.Skip(skip).Take(take).ToListAsync(ct);
        int totalCount;

        if (items.Count < take && items.Count > 0)
        {
            totalCount = skip + items.Count;
        }
        else if (items.Count == 0 && skip == 0)
        {
            totalCount = 0;
        }
        else
        {
            totalCount = await source.CountAsync(ct);
        }

        return new PagedList<T>(items, totalCount, skip, take);
    }

    /// <summary>
    /// Projects, materializes a page of items, and returns paging metadata.
    /// </summary>
    public static async Task<PagedList<TResult>> ToPagedListAsync<TSource, TResult>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, TResult>> selector,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var items = await source
            .Select(selector)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        int totalCount;
        if (items.Count < take && items.Count > 0)
        {
            totalCount = skip + items.Count;
        }
        else if (items.Count == 0 && skip == 0)
        {
            totalCount = 0;
        }
        else
        {
            totalCount = await source.CountAsync(ct);
        }

        return new PagedList<TResult>(items, totalCount, skip, take);
    }

    /// <summary>
    /// Materializes items for cursor-based pagination.
    /// </summary>
    public static async Task<CursorList<TSource>> ToCursorListAsync<TSource>(
        this IQueryable<TSource> source,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var items = await source.Skip(skip).Take(take + 1).ToListAsync(ct);
        return new CursorList<TSource>(items, skip, take);
    }

    /// <summary>
    /// Projects and materializes items for cursor-based pagination.
    /// </summary>
    public static async Task<CursorList<TResult>> ToCursorListAsync<TSource, TResult>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, TResult>> selector,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var items = await source
            .Select(selector)
            .Skip(skip)
            .Take(take + 1)
            .ToListAsync(ct);

        return new CursorList<TResult>(items, skip, take);
    }
}

public interface IPagedList
{
    int Total { get; }
    int Skip { get; }
    int Take { get; }
    IEnumerable<SortInfo> Sort { get; }
}

public record SortInfo(string Field, bool IsAsc);

public record PagedList<TItem>(
    IEnumerable<TItem> Items,
    int Total,
    int Skip,
    int Take,
    IEnumerable<SortInfo>? Sort = null
) : IPagedList
{
    // Secondary constructor preserves logic: take ?? items.Count()
    public PagedList(
        IEnumerable<TItem> items,
        int total,
        int skip = 0,
        int? take = null,
        IEnumerable<SortInfo>? sort = null)
        : this(items, total, skip, take ?? items.Count(), sort)
    {
    }

    // Parameterless constructor for serialization (initializes empty defaults)
    public PagedList()
        : this([], 0, 0, 0)
    {
    }
}

public record CursorList<TItem>(
    IEnumerable<TItem> Items,
    bool HasNextPage,
    int Skip,
    int Take
)
{
    /// <summary>
    /// Constructor that detects HasNextPage based on whether the input collection
    /// contains more items than the 'take' limit.
    /// </summary>
    /// <param name="items">The materialized list (should contain up to take + 1 items).</param>
    public CursorList(IReadOnlyCollection<TItem> items, int skip, int take)
        : this(
            items.Take(take),          // Slice the list to the requested size
            items.Count > take,        // Check if we fetched an extra item
            skip,
            take
          )
    {
    }

    public CursorList() : this([], false, 0, 0) { }
}