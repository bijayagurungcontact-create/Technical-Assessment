using BookStore.Application.Abstractions;
using BookStore.Application.Common.Exceptions;
using BookStore.Application.Common.Messaging;
using BookStore.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Books.AddBook;

internal sealed class AddBookHandler(
    IBookStoreDbContext db,
    TimeProvider timeProvider,
    ILogger<AddBookHandler> logger) : IRequestHandler<AddBookCommand, BookResponse>
{
    public async Task<BookResponse> Handle(AddBookCommand command, CancellationToken cancellationToken)
    {
        var openLibraryKey = command.OpenLibraryKey.Trim();
        var isbn = Book.NormalizeIsbn(command.Isbn);

        // Friendly check first; the unique indexes on OpenLibraryKey/Isbn are the real guarantee
        // against races (a violation there is also surfaced as a ConflictException).
        var duplicate = await db.Books
            .AsNoTracking()
            .Where(b => b.OpenLibraryKey == openLibraryKey || (isbn != null && b.Isbn == isbn))
            .Select(b => new { b.Id, b.Title })
            .FirstOrDefaultAsync(cancellationToken);

        if (duplicate is not null)
        {
            throw new ConflictException($"'{duplicate.Title}' is already in the store.");
        }

        var book = Book.Create(
            command.Title,
            command.Author,
            isbn,
            command.PublishedYear,
            command.CoverUrl,
            openLibraryKey,
            command.Description,
            command.Genre,
            command.Price,
            command.Stock,
            timeProvider.GetUtcNow().UtcDateTime);

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Added book {BookId} ({OpenLibraryKey}) to the store in genre {Genre}",
            book.Id,
            book.OpenLibraryKey,
            book.Genre);

        return BookResponse.FromEntity(book);
    }
}
