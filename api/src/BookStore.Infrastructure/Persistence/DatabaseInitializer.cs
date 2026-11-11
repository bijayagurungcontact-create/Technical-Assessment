using BookStore.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookStore.Infrastructure.Persistence;

/// <summary>Applies pending migrations and seeds a few real Open Library books on first run.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BookStoreDbContext>>();

        await db.Database.MigrateAsync(cancellationToken);

        if (await db.Books.AnyAsync(cancellationToken))
        {
            return;
        }

        var books = SeedBooks(TimeProvider.System.GetUtcNow().UtcDateTime);
        db.Books.AddRange(books);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {BookCount} sample books", books.Count);
    }

    // Catalog fields were captured from the Open Library Search API, exactly as the add-book flow would.
    private static List<Book> SeedBooks(DateTime addedAtUtc)
    {
        Book Seed(string title, string author, string isbn, int year, int coverId, string key, Genre genre, decimal price, int stock) =>
            Book.Create(
                title,
                author,
                isbn,
                year,
                $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg",
                key,
                description: null,
                genre,
                price,
                stock,
                addedAtUtc);

        return
        [
            Seed("Dune", "Frank Herbert", "9780425043769", 1965, 11481354, "/works/OL893414W", Genre.SciFi, 18.99m, 12),
            Seed("The Hobbit", "J.R.R. Tolkien", "3763252088", 1937, 14627509, "/works/OL27482W", Genre.Fantasy, 14.50m, 8),
            Seed("And Then There Were None", "Agatha Christie", "2702478581", 1939, 11172296, "/works/OL471565W", Genre.Mystery, 9.99m, 15),
            Seed("Sapiens", "Yuval Noah Harari", "5905891648", 2011, 8634250, "/works/OL17075811W", Genre.NonFiction, 22.00m, 6),
            Seed("Steve Jobs", "Walter Isaacson", "357010124X", 2011, 12374726, "/works/OL16085155W", Genre.Biography, 19.95m, 4),
            Seed("Charlotte's Web", "E. B. White", "9789750821714", 1952, 8461797, "/works/OL483391W", Genre.Children, 7.99m, 20),
            Seed("To Kill a Mockingbird", "Harper Lee", "9780060173227", 1960, 14351077, "/works/OL3140822W", Genre.Fiction, 12.49m, 10),
            Seed("The Guns of August", "Barbara Tuchman", "0553026690", 1962, 6616967, "/works/OL3378527W", Genre.History, 16.75m, 3),
        ];
    }
}
