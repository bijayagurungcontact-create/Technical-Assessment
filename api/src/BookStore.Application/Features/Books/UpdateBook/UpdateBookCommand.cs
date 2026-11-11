using BookStore.Application.Common.Messaging;
using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books.UpdateBook;

/// <summary>Changes the store-owned fields only. Catalog fields are immutable once added.</summary>
public sealed record UpdateBookCommand(
    Guid Id,
    Genre Genre,
    decimal Price,
    int Stock,
    string? Description) : ICommand<BookResponse>;
