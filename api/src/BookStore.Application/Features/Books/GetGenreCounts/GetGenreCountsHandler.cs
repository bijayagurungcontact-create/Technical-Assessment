using BookStore.Application.Abstractions;
using BookStore.Application.Common.Messaging;
using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books.GetGenreCounts;

internal sealed class GetGenreCountsHandler(IGenreStatistics statistics)
    : IRequestHandler<GetGenreCountsQuery, IReadOnlyList<GenreCountResponse>>
{
    public async Task<IReadOnlyList<GenreCountResponse>> Handle(GetGenreCountsQuery query, CancellationToken cancellationToken)
    {
        var counts = (await statistics.GetBookCountsByGenreAsync(cancellationToken))
            .ToDictionary(c => c.Genre, c => c.BookCount);

        // The SQL only returns genres that have books; the filter should list every genre.
        return Enum.GetValues<Genre>()
            .Select(genre => new GenreCountResponse(genre, counts.GetValueOrDefault(genre)))
            .ToList();
    }
}
