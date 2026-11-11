using BookStore.Application.Abstractions;
using BookStore.Application.Common.Messaging;
using BookStore.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Application.Features.Books.GetBooks;

internal sealed class GetBooksHandler(IBookStoreDbContext db)
    : IRequestHandler<GetBooksQuery, PagedResult<BookResponse>>
{
    public async Task<PagedResult<BookResponse>> Handle(GetBooksQuery query, CancellationToken cancellationToken)
    {
        var books = db.Books.AsNoTracking();

        if (query.Genre is { } genre)
        {
            books = books.Where(b => b.Genre == genre);
        }

        var totalCount = await books.CountAsync(cancellationToken);

        var page = await BookSorting.Apply(books, query.SortBy, query.SortDirection)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BookResponse>(
            page.Select(BookResponse.FromEntity).ToList(),
            query.Page,
            query.PageSize,
            totalCount);
    }
}
