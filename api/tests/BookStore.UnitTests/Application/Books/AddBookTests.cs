using System.Globalization;
using BookStore.Application.Common.Exceptions;
using BookStore.Domain.Books;
using BookStore.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BookStore.UnitTests.Application.Books;

public sealed class AddBookTests
{
    [Fact]
    public async Task Persists_catalog_fields_together_with_store_fields()
    {
        await using var app = new TestApplication();

        var created = await app.Send(TestBooks.AddCommand());

        var stored = await app.Query(db => db.Books.SingleAsync(b => b.Id == created.Id));
        Assert.Equal("/works/OL893414W", stored.OpenLibraryKey);
        Assert.Equal("9780441172719", stored.Isbn);
        Assert.Equal("Frank Herbert", stored.Author);
        Assert.Equal(Genre.SciFi, stored.Genre);
        Assert.Equal(18.99m, stored.Price);
        Assert.Equal(5, stored.Stock);
    }

    [Fact]
    public async Task Rejects_a_book_whose_open_library_key_is_already_in_the_store()
    {
        await using var app = new TestApplication();
        await app.Send(TestBooks.AddCommand(key: "/works/OL1W", isbn: null));

        await Assert.ThrowsAsync<ConflictException>(() => app.Send(TestBooks.AddCommand(key: "/works/OL1W", isbn: null)));
    }

    [Fact]
    public async Task Rejects_a_book_whose_isbn_is_already_in_the_store_even_if_formatted_differently()
    {
        await using var app = new TestApplication();
        await app.Send(TestBooks.AddCommand(key: "/works/OL1W", isbn: "9780441172719"));

        await Assert.ThrowsAsync<ConflictException>(
            () => app.Send(TestBooks.AddCommand(key: "/works/OL2W", isbn: "978-0-441-17271-9")));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("10.123")]
    [InlineData("100000.01")]
    public async Task Rejects_invalid_prices(string price)
    {
        await using var app = new TestApplication();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => app.Send(TestBooks.AddCommand(price: decimal.Parse(price, CultureInfo.InvariantCulture))));

        Assert.Contains("Price", ex.Errors.Keys);
        Assert.Equal(0, await app.Query(db => db.Books.CountAsync()));
    }

    [Theory]
    [InlineData("https://evil.example.com/cover.jpg")]
    [InlineData("http://covers.openlibrary.org/b/id/1-M.jpg")]
    public async Task Rejects_cover_urls_that_are_not_open_library_https(string coverUrl)
    {
        await using var app = new TestApplication();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => app.Send(TestBooks.AddCommand(coverUrl: coverUrl)));

        Assert.Contains("CoverUrl", ex.Errors.Keys);
    }

    [Fact]
    public async Task Rejects_malformed_open_library_keys()
    {
        await using var app = new TestApplication();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => app.Send(TestBooks.AddCommand(key: "not-a-key")));

        Assert.Contains("OpenLibraryKey", ex.Errors.Keys);
    }
}
