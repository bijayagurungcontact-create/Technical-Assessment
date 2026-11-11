using BookStore.Application.Abstractions;

namespace BookStore.UnitTests.TestSupport;

internal sealed class FakeBookCatalog(params CatalogBook[] results) : IBookCatalog
{
    public Task<CatalogSearchPage> SearchAsync(string term, int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult(new CatalogSearchPage(results, results.Length));
}
