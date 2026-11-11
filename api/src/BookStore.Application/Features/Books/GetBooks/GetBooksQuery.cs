using BookStore.Application.Common.Messaging;
using BookStore.Application.Common.Models;
using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books.GetBooks;

public sealed record GetBooksQuery(
    Genre? Genre = null,
    int Page = 1,
    int PageSize = GetBooksQuery.DefaultPageSize,
    string? SortBy = null,
    string? SortDirection = null) : IQuery<PagedResult<BookResponse>>
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
}
