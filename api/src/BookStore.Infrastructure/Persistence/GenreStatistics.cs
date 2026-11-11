using BookStore.Application.Abstractions;
using BookStore.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Persistence;

/// <summary>
/// Hand-written SQL against SQLite: books grouped by genre with a count per genre.
/// Powers the header genre filter (GET /api/genres).
/// </summary>
internal sealed class GenreStatistics(BookStoreDbContext db) : IGenreStatistics
{
    private const string BookCountsByGenreSql =
        """
        SELECT b.Genre    AS Genre,
               COUNT(*)   AS BookCount
        FROM   Books AS b
        GROUP  BY b.Genre
        ORDER  BY b.Genre
        """;

    public async Task<IReadOnlyList<GenreCount>> GetBookCountsByGenreAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Database
            .SqlQueryRaw<GenreCountRow>(BookCountsByGenreSql)
            .ToListAsync(cancellationToken);

        // Ignore any value that is not part of the controlled vocabulary (e.g. a genre removed from the enum).
        var counts = new List<GenreCount>(rows.Count);
        foreach (var row in rows)
        {
            if (Enum.TryParse<Genre>(row.Genre, out var genre))
            {
                counts.Add(new GenreCount(genre, (int)row.BookCount));
            }
        }

        return counts;
    }

    // Column names in the SQL above map onto these properties.
    private sealed class GenreCountRow
    {
        public string Genre { get; init; } = "";

        public long BookCount { get; init; }
    }
}
