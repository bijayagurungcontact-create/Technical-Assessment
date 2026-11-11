using BookStore.Application.Abstractions;
using BookStore.Application.Common.Exceptions;
using BookStore.Application.Features.Catalog.SearchCatalog;
using BookStore.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BookStore.UnitTests.Application.Catalog;

public sealed class SearchCatalogTests
{
    [Fact]
    public async Task Flags_results_already_in_the_store_and_persists_nothing()
    {
        var catalog = new FakeBookCatalog(
            new CatalogBook("/works/OL1W", "Owned by key", "A", null, 2000, null),
            new CatalogBook("/works/OL2W", "Owned by ISBN", "B", "9780441172719", 2000, null),
            new CatalogBook("/works/OL3W", "New", "C", "9780000000002", 2000, null));
        await using var app = new TestApplication(catalog);
        await app.Seed(
            TestBooks.Create(key: "/works/OL1W"),
            TestBooks.Create(key: "/works/OL99W", isbn: "978-0-441-17271-9"));

        var result = await app.Send(new SearchCatalogQuery("anything"));

        Assert.Equal([true, true, false], result.Items.Select(i => i.AlreadyInStore));
        Assert.Equal(2, await app.Query(db => db.Books.CountAsync()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" a ")]
    public async Task Rejects_blank_or_too_short_terms(string term)
    {
        await using var app = new TestApplication();

        await Assert.ThrowsAsync<RequestValidationException>(() => app.Send(new SearchCatalogQuery(term)));
    }
}
