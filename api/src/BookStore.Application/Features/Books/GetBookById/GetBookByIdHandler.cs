using BookStore.Application.Abstractions;
using BookStore.Application.Common.Exceptions;
using BookStore.Application.Common.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Application.Features.Books.GetBookById;

internal sealed class GetBookByIdHandler(IBookStoreDbContext db) : IRequestHandler<GetBookByIdQuery, BookResponse>
{
    public async Task<BookResponse> Handle(GetBookByIdQuery query, CancellationToken cancellationToken)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == query.Id, cancellationToken)
            ?? throw new NotFoundException("Book", query.Id);

        return BookResponse.FromEntity(book);
    }
}
