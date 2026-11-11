using BookStore.Application.Abstractions;
using BookStore.Application.Common.Exceptions;
using BookStore.Application.Common.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Features.Books.DeleteBook;

internal sealed class DeleteBookHandler(IBookStoreDbContext db, ILogger<DeleteBookHandler> logger)
    : IRequestHandler<DeleteBookCommand, Unit>
{
    public async Task<Unit> Handle(DeleteBookCommand command, CancellationToken cancellationToken)
    {
        var deleted = await db.Books.Where(b => b.Id == command.Id).ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            throw new NotFoundException("Book", command.Id);
        }

        logger.LogInformation("Deleted book {BookId}", command.Id);
        return Unit.Value;
    }
}
