using BookStore.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Application.Abstractions;

/// <summary>
/// The store's unit of work. Handlers query and modify books through EF Core directly instead of
/// through a generic repository: DbContext already *is* a repository + unit of work, and each
/// vertical slice owns the exact query it needs. The concrete SQLite context lives in Infrastructure.
/// </summary>
public interface IBookStoreDbContext
{
    DbSet<Book> Books { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
