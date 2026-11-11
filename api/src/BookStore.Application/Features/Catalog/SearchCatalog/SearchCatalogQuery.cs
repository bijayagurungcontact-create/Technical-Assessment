using BookStore.Application.Common.Messaging;

namespace BookStore.Application.Features.Catalog.SearchCatalog;

/// <summary>Searches the external catalog. Results are transient: nothing here is persisted.</summary>
public sealed record SearchCatalogQuery(string Term, int Page = 1, int PageSize = SearchCatalogQuery.DefaultPageSize)
    : IQuery<CatalogSearchResponse>
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 20;
}

public sealed record CatalogSearchResponse(
    IReadOnlyList<CatalogBookResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <param name="AlreadyInStore">True when this work is already a store book, so the UI can disable it.</param>
public sealed record CatalogBookResponse(
    string OpenLibraryKey,
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl,
    bool AlreadyInStore);
