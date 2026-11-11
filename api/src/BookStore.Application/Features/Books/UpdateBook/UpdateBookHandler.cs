using BookStore.Application.Abstractions;
using BookStore.Application.Common.Exceptions;
using BookStore.Application.Common.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Application.Features.Books.UpdateBook;

internal sealed class UpdateBookHandler(IBookStoreDbContext db, TimeProvider timeProvider)
    : IRequestHandler<UpdateBookCommand, BookResponse>
{
    public async Task<BookResponse> Handle(UpdateBookCommand command, CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Book", command.Id);

        book.UpdateStoreDetails(
            command.Genre,
            command.Price,
            command.Stock,
            command.Description,
            timeProvider.GetUtcNow().UtcDateTime);

        await db.SaveChangesAsync(cancellationToken);
        return BookResponse.FromEntity(book);
    }
}
