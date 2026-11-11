using BookStore.Domain.Books;
using BookStore.Domain.Common;
using BookStore.UnitTests.TestSupport;

namespace BookStore.UnitTests.Domain;

public sealed class BookTests
{
    [Theory]
    [InlineData("978-0-441-17271-9", "9780441172719")]
    [InlineData("0 441 17271 x", "044117271X")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void NormalizeIsbn_strips_separators(string? input, string? expected) =>
        Assert.Equal(expected, Book.NormalizeIsbn(input));

    [Fact]
    public void Create_rejects_negative_price() =>
        Assert.Throws<DomainException>(() => TestBooks.Create(price: -1m));

    [Fact]
    public void UpdateStoreDetails_changes_only_store_fields_and_stamps_update_time()
    {
        var book = TestBooks.Create(title: "Dune", genre: Genre.SciFi, price: 10m, stock: 1);
        var updatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        book.UpdateStoreDetails(Genre.Fiction, 12.345m, 7, "  A classic.  ", updatedAt);

        Assert.Equal("Dune", book.Title);
        Assert.Equal(Genre.Fiction, book.Genre);
        Assert.Equal(12.35m, book.Price);
        Assert.Equal(7, book.Stock);
        Assert.Equal("A classic.", book.Description);
        Assert.Equal(updatedAt, book.UpdatedAtUtc);
    }
}
