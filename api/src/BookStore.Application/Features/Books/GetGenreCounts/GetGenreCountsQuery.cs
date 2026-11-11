using BookStore.Application.Common.Messaging;
using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books.GetGenreCounts;

/// <summary>Every genre with the number of store books in it. Powers the header filter dropdown.</summary>
public sealed record GetGenreCountsQuery : IQuery<IReadOnlyList<GenreCountResponse>>;

public sealed record GenreCountResponse(Genre Genre, int BookCount);
