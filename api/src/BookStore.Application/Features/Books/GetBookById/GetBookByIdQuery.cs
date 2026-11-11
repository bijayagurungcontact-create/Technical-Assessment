using BookStore.Application.Common.Messaging;

namespace BookStore.Application.Features.Books.GetBookById;

public sealed record GetBookByIdQuery(Guid Id) : IQuery<BookResponse>;
