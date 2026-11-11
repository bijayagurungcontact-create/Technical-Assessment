using BookStore.Application.Common.Exceptions;
using BookStore.Application.Features.Books.GetBooks;
using BookStore.Domain.Books;
using BookStore.UnitTests.TestSupport;

namespace BookStore.UnitTests.Application.Books;

public sealed class GetBooksTests
{
    [Fact]
    public async Task Filters_by_genre()
    {
        await using var app = new TestApplication();
        await app.Seed(
            TestBooks.Create("Dune", Genre.SciFi),
            TestBooks.Create("Foundation", Genre.SciFi),
            TestBooks.Create("The Hobbit", Genre.Fantasy));

        var result = await app.Send(new GetBooksQuery(Genre: Genre.SciFi));

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, b => Assert.Equal(Genre.SciFi, b.Genre));
    }

    [Fact]
    public async Task Sorts_by_price_descending_in_the_database_and_pages()
    {
        await using var app = new TestApplication();
        await app.Seed(
            TestBooks.Create("Cheap", price: 5m),
            TestBooks.Create("Mid", price: 20m),
            TestBooks.Create("Pricey", price: 100m),
            TestBooks.Create("Mid-cheap", price: 9.99m));

        var firstPage = await app.Send(new GetBooksQuery(Page: 1, PageSize: 2, SortBy: "price", SortDirection: "desc"));
        var secondPage = await app.Send(new GetBooksQuery(Page: 2, PageSize: 2, SortBy: "price", SortDirection: "desc"));

        Assert.Equal(["Pricey", "Mid"], firstPage.Items.Select(b => b.Title));
        Assert.Equal(["Mid-cheap", "Cheap"], secondPage.Items.Select(b => b.Title));
        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
    }

    [Fact]
    public async Task Defaults_to_newest_first()
    {
        await using var app = new TestApplication();
        await app.Seed(
            TestBooks.Create("Old", addedAtUtc: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            TestBooks.Create("New", addedAtUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await app.Send(new GetBooksQuery());

        Assert.Equal(["New", "Old"], result.Items.Select(b => b.Title));
    }

    [Theory]
    [InlineData("isbn; DROP TABLE Books", null)]
    [InlineData("title", "sideways")]
    public async Task Rejects_unknown_sort_parameters(string sortBy, string? direction)
    {
        await using var app = new TestApplication();

        await Assert.ThrowsAsync<RequestValidationException>(
            () => app.Send(new GetBooksQuery(SortBy: sortBy, SortDirection: direction)));
    }
}
