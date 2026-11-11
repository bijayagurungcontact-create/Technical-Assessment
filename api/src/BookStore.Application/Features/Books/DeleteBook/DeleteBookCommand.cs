using BookStore.Application.Common.Messaging;

namespace BookStore.Application.Features.Books.DeleteBook;

public sealed record DeleteBookCommand(Guid Id) : ICommand<Unit>;
