using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books;

/// <summary>The store's view of a book, shared by every slice in the Books feature.</summary>
public sealed record BookResponse(
    Guid Id,
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl,
    string OpenLibraryKey,
    string? Description,
    Genre Genre,
    decimal Price,
    int Stock,
    DateTime AddedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static BookResponse FromEntity(Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.Isbn,
        book.PublishedYear,
        book.CoverUrl,
        book.OpenLibraryKey,
        book.Description,
        book.Genre,
        book.Price,
        book.Stock,
        book.AddedAtUtc,
        book.UpdatedAtUtc);
}
