using BookStore.Application.Common.Messaging;
using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books.AddBook;

/// <summary>
/// Persists a book the user picked from a catalog search, combined with the store-owned fields.
/// The catalog fields are echoed back from the search result the client received.
/// </summary>
public sealed record AddBookCommand(
    string OpenLibraryKey,
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl,
    string? Description,
    Genre Genre,
    decimal Price,
    int Stock = 0) : ICommand<BookResponse>;
