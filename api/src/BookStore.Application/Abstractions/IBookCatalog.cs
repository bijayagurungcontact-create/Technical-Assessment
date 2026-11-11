namespace BookStore.Application.Abstractions;

/// <summary>A transient search hit from the external catalog. Never persisted as-is.</summary>
public sealed record CatalogBook(
    string OpenLibraryKey,
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl);

public sealed record CatalogSearchPage(IReadOnlyList<CatalogBook> Items, int TotalCount);

/// <summary>
/// Port to the external book catalog used to *discover* books (Open Library today).
/// Implementations must throw <see cref="Common.Exceptions.CatalogUnavailableException"/>
/// when the catalog cannot be reached, so callers never see transport-specific exceptions.
/// </summary>
public interface IBookCatalog
{
    Task<CatalogSearchPage> SearchAsync(string term, int page, int pageSize, CancellationToken cancellationToken);
}
