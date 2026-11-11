using BookStore.Application.Features.Books.AddBook;
using BookStore.Domain.Books;

namespace BookStore.UnitTests.TestSupport;

/// <summary>Test data builders.</summary>
internal static class TestBooks
{
    private static int _sequence = 1000;

    public static Book Create(
        string title = "Dune",
        Genre genre = Genre.SciFi,
        decimal price = 10m,
        int stock = 1,
        string? isbn = null,
        string? key = null,
        DateTime? addedAtUtc = null)
    {
        var id = Interlocked.Increment(ref _sequence);
        return Book.Create(
            title,
            "Some Author",
            isbn,
            2000,
            coverUrl: null,
            key ?? $"/works/OL{id}W",
            description: null,
            genre,
            price,
            stock,
            addedAtUtc ?? DateTime.UtcNow);
    }

    public static AddBookCommand AddCommand(
        string key = "/works/OL893414W",
        string? isbn = "978-0-441-17271-9",
        decimal price = 18.99m,
        string? coverUrl = "https://covers.openlibrary.org/b/id/11481354-M.jpg") =>
        new(key, "Dune", "Frank Herbert", isbn, 1965, coverUrl, null, Genre.SciFi, price, 5);
}
