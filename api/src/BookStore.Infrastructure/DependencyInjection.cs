using BookStore.Application.Abstractions;
using BookStore.Infrastructure.Catalog.OpenLibrary;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace BookStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BookStore")
            ?? throw new InvalidOperationException("Connection string 'BookStore' is not configured.");

        services.AddDbContext<BookStoreDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IBookStoreDbContext>(sp => sp.GetRequiredService<BookStoreDbContext>());
        services.AddScoped<IGenreStatistics, GenreStatistics>();

        services.AddOpenLibrary(configuration);

        return services;
    }

    private static void AddOpenLibrary(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenLibraryOptions>()
            .Bind(configuration.GetSection(OpenLibraryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddHttpClient<IBookCatalog, OpenLibraryBookCatalog>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<OpenLibraryOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                // Resilience pipeline below owns the timeouts.
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler()
            .Configure((HttpStandardResilienceOptions resilience, IServiceProvider sp) =>
            {
                var options = sp.GetRequiredService<IOptions<OpenLibraryOptions>>().Value;
                var attemptTimeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds);

                resilience.AttemptTimeout.Timeout = attemptTimeout;
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds);
                resilience.Retry.MaxRetryAttempts = 1;
                resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 4;
            });
    }
}
