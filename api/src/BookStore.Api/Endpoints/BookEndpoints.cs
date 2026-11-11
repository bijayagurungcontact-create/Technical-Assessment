using BookStore.Application.Common.Messaging;
using BookStore.Application.Common.Models;
using BookStore.Application.Features.Books;
using BookStore.Application.Features.Books.AddBook;
using BookStore.Application.Features.Books.DeleteBook;
using BookStore.Application.Features.Books.GetBookById;
using BookStore.Application.Features.Books.GetBooks;
using BookStore.Application.Features.Books.GetGenreCounts;
using BookStore.Application.Features.Books.UpdateBook;
using BookStore.Domain.Books;

namespace BookStore.Api.Endpoints;

/// <summary>
/// Thin HTTP adapters: bind the request, send it through the mediator, shape the HTTP result.
/// No business logic lives here. None of these endpoints touch Open Library.
/// </summary>
internal static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var books = app.MapGroup("/api/books").WithTags("Books");

        books.MapGet("/", GetBooks).WithName(nameof(GetBooks));
        books.MapGet("/{id:guid}", GetBookById).WithName(nameof(GetBookById));
        books.MapPost("/", AddBook).WithName(nameof(AddBook));
        books.MapPut("/{id:guid}", UpdateBook).WithName(nameof(UpdateBook));
        books.MapDelete("/{id:guid}", DeleteBook).WithName(nameof(DeleteBook));

        app.MapGet("/api/genres", GetGenreCounts).WithTags("Books").WithName(nameof(GetGenreCounts));

        return app;
    }

    private static Task<PagedResult<BookResponse>> GetBooks(
        ISender sender,
        CancellationToken ct,
        Genre? genre = null,
        int page = 1,
        int pageSize = GetBooksQuery.DefaultPageSize,
        string? sortBy = null,
        string? sortDirection = null) =>
        sender.Send(new GetBooksQuery(genre, page, pageSize, sortBy, sortDirection), ct);

    private static Task<BookResponse> GetBookById(Guid id, ISender sender, CancellationToken ct) =>
        sender.Send(new GetBookByIdQuery(id), ct);

    private static async Task<IResult> AddBook(AddBookCommand command, ISender sender, CancellationToken ct)
    {
        var book = await sender.Send(command, ct);
        return TypedResults.CreatedAtRoute(book, nameof(GetBookById), new { id = book.Id });
    }

    private static Task<BookResponse> UpdateBook(Guid id, UpdateBookRequest request, ISender sender, CancellationToken ct) =>
        sender.Send(new UpdateBookCommand(id, request.Genre, request.Price, request.Stock, request.Description), ct);

    private static async Task<IResult> DeleteBook(Guid id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new DeleteBookCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static Task<IReadOnlyList<GenreCountResponse>> GetGenreCounts(ISender sender, CancellationToken ct) =>
        sender.Send(new GetGenreCountsQuery(), ct);

    /// <summary>PUT body: only store-owned fields are editable.</summary>
    internal sealed record UpdateBookRequest(Genre Genre, decimal Price, int Stock, string? Description);
}
