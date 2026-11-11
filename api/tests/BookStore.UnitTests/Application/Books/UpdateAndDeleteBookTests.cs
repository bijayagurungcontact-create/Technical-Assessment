using BookStore.Application.Common.Exceptions;
using BookStore.Application.Features.Books.DeleteBook;
using BookStore.Application.Features.Books.UpdateBook;
using BookStore.Domain.Books;
using BookStore.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BookStore.UnitTests.Application.Books;

public sealed class UpdateAndDeleteBookTests
{
    [Fact]
    public async Task Update_changes_store_fields()
    {
        await using var app = new TestApplication();
        var book = TestBooks.Create(genre: Genre.SciFi, price: 10m, stock: 1);
        await app.Seed(book);

        var updated = await app.Send(new UpdateBookCommand(book.Id, Genre.Fiction, 15.5m, 3, "Desert planet."));

        Assert.Equal(Genre.Fiction, updated.Genre);
        Assert.Equal(15.5m, updated.Price);
        Assert.Equal(3, updated.Stock);
        Assert.NotNull(updated.UpdatedAtUtc);
        Assert.Equal(15.5m, await app.Query(db => db.Books.Where(b => b.Id == book.Id).Select(b => b.Price).SingleAsync()));
    }

    [Fact]
    public async Task Update_of_unknown_book_is_not_found()
    {
        await using var app = new TestApplication();

        await Assert.ThrowsAsync<NotFoundException>(
            () => app.Send(new UpdateBookCommand(Guid.NewGuid(), Genre.Fiction, 1m, 1, null)));
    }

    [Fact]
    public async Task Delete_removes_the_book()
    {
        await using var app = new TestApplication();
        var book = TestBooks.Create();
        await app.Seed(book);

        await app.Send(new DeleteBookCommand(book.Id));

        Assert.False(await app.Query(db => db.Books.AnyAsync(b => b.Id == book.Id)));
    }

    [Fact]
    public async Task Delete_of_unknown_book_is_not_found()
    {
        await using var app = new TestApplication();

        await Assert.ThrowsAsync<NotFoundException>(() => app.Send(new DeleteBookCommand(Guid.NewGuid())));
    }
}
