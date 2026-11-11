using BookStore.Application.Abstractions;
using BookStore.Application.Common.Messaging;
using BookStore.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Application.Features.Catalog.SearchCatalog;

internal sealed class SearchCatalogHandler(IBookCatalog catalog, IBookStoreDbContext db)
    : IRequestHandler<SearchCatalogQuery, CatalogSearchResponse>
{
    public async Task<CatalogSearchResponse> Handle(SearchCatalogQuery query, CancellationToken cancellationToken)
    {
        var result = await catalog.SearchAsync(query.Term.Trim(), query.Page, query.PageSize, cancellationToken);

        // Flag hits the store already owns so the UI can prevent obvious duplicates up front.
        var keys = result.Items.Select(i => i.OpenLibraryKey).ToList();
        var isbns = result.Items.Select(i => Book.NormalizeIsbn(i.Isbn)).OfType<string>().ToList();

        var owned = await db.Books
            .AsNoTracking()
            .Where(b => keys.Contains(b.OpenLibraryKey) || (b.Isbn != null && isbns.Contains(b.Isbn)))
            .Select(b => new { b.OpenLibraryKey, b.Isbn })
            .ToListAsync(cancellationToken);

        var ownedKeys = owned.Select(o => o.OpenLibraryKey).ToHashSet();
        var ownedIsbns = owned.Select(o => o.Isbn).OfType<string>().ToHashSet();

        var items = result.Items
            .Select(i => new CatalogBookResponse(
                i.OpenLibraryKey,
                i.Title,
                i.Author,
                i.Isbn,
                i.PublishedYear,
                i.CoverUrl,
                AlreadyInStore: ownedKeys.Contains(i.OpenLibraryKey)
                    || (Book.NormalizeIsbn(i.Isbn) is { } isbn && ownedIsbns.Contains(isbn))))
            .ToList();

        return new CatalogSearchResponse(items, query.Page, query.PageSize, result.TotalCount);
    }
}
