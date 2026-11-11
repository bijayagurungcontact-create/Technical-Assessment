using BookStore.Application.Features.Books.GetGenreCounts;
using BookStore.Domain.Books;
using BookStore.UnitTests.TestSupport;

namespace BookStore.UnitTests.Application.Books;

/// <summary>Exercises the hand-written GROUP BY SQL against a real SQLite database.</summary>
public sealed class GetGenreCountsTests
{
    [Fact]
    public async Task Returns_every_genre_with_its_book_count()
    {
        await using var app = new TestApplication();
        await app.Seed(
            TestBooks.Create(genre: Genre.SciFi),
            TestBooks.Create(genre: Genre.SciFi),
            TestBooks.Create(genre: Genre.Mystery));

        var counts = await app.Send(new GetGenreCountsQuery());

        Assert.Equal(Enum.GetValues<Genre>().Length, counts.Count);
        Assert.Equal(2, counts.Single(c => c.Genre == Genre.SciFi).BookCount);
        Assert.Equal(1, counts.Single(c => c.Genre == Genre.Mystery).BookCount);
        Assert.Equal(0, counts.Single(c => c.Genre == Genre.History).BookCount);
    }
}
