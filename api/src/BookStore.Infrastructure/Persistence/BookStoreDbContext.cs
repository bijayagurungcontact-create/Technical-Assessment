using BookStore.Application.Abstractions;
using BookStore.Application.Common.Exceptions;
using BookStore.Domain.Books;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Persistence;

public sealed class BookStoreDbContext(DbContextOptions<BookStoreDbContext> options)
    : DbContext(options), IBookStoreDbContext
{
    private const int SqliteConstraintViolation = 19;

    public DbSet<Book> Books => Set<Book>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintViolation } sqlite
                                           && sqlite.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
        {
            // Backstop for the race the handler's pre-check cannot cover.
            throw new ConflictException("This book is already in the store.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookStoreDbContext).Assembly);
}
