using BookStore.Application;
using BookStore.Application.Abstractions;
using BookStore.Application.Common.Messaging;
using BookStore.Domain.Books;
using BookStore.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.UnitTests.TestSupport;

/// <summary>
/// The real Application layer (mediator, behaviors, validators, handlers) wired to a private
/// in-memory SQLite database, so slices are exercised end-to-end minus HTTP. SQLite rather than
/// the EF InMemory provider so unique indexes, raw SQL and value conversions behave like production.
/// </summary>
internal sealed class TestApplication : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public TestApplication(IBookCatalog? catalog = null)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddApplication();
        services.AddDbContext<BookStoreDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<IBookStoreDbContext>(sp => sp.GetRequiredService<BookStoreDbContext>());
        services.AddScoped<IGenreStatistics, GenreStatistics>();
        services.AddSingleton(catalog ?? new FakeBookCatalog());

        _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        using var scope = _services.CreateScope();
        scope.ServiceProvider.GetRequiredService<BookStoreDbContext>().Database.EnsureCreated();
    }

    /// <summary>Each call gets a fresh scope, like a separate HTTP request.</summary>
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    public async Task<T> Query<T>(Func<BookStoreDbContext, Task<T>> query)
    {
        await using var scope = _services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<BookStoreDbContext>());
    }

    public async Task Seed(params Book[] books)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        db.Books.AddRange(books);
        await db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
