using BookStore.Domain.Books;

namespace BookStore.Application.Abstractions;

public sealed record GenreCount(Genre Genre, int BookCount);

/// <summary>
/// Aggregate read over the store. Implemented in Infrastructure with hand-written SQL because the
/// query is provider-specific; the Application layer only cares about the result shape.
/// </summary>
public interface IGenreStatistics
{
    Task<IReadOnlyList<GenreCount>> GetBookCountsByGenreAsync(CancellationToken cancellationToken);
}
