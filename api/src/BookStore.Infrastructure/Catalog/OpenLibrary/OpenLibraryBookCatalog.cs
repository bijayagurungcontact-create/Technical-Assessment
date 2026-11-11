using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using BookStore.Application.Abstractions;
using BookStore.Application.Common.Exceptions;
using BookStore.Domain.Books;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace BookStore.Infrastructure.Catalog.OpenLibrary;

/// <summary>
/// <see cref="IBookCatalog"/> backed by the Open Library Search API. Timeouts, retries and the
/// circuit breaker are configured on the typed HttpClient (see DependencyInjection); this class
/// builds the request, normalizes the response and turns transport failures into
/// <see cref="CatalogUnavailableException"/>.
/// </summary>
internal sealed class OpenLibraryBookCatalog(
    HttpClient httpClient,
    IOptions<OpenLibraryOptions> options,
    ILogger<OpenLibraryBookCatalog> logger) : IBookCatalog
{
    private const string SearchFields = "key,title,author_name,first_publish_year,isbn,cover_i";

    public async Task<CatalogSearchPage> SearchAsync(string term, int page, int pageSize, CancellationToken cancellationToken)
    {
        var requestUri = BuildSearchUri(term, page, pageSize);
        var startedAt = Stopwatch.GetTimestamp();

        OpenLibrarySearchResponse? response;
        try
        {
            response = await httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(requestUri, cancellationToken);
        }
        catch (Exception ex) when (IsTransientFailure(ex, cancellationToken))
        {
            logger.LogWarning(
                ex,
                "Open Library search for {SearchTerm} failed after {ElapsedMs:0} ms",
                term,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

            throw new CatalogUnavailableException("Open Library is currently unavailable. Please try again shortly.", ex);
        }

        var searchedIsbn = TryGetIsbn(term);
        var items = (response?.Docs ?? [])
            .Select(doc => Normalize(doc, searchedIsbn))
            .OfType<CatalogBook>()
            .ToList();

        logger.LogInformation(
            "Open Library search for {SearchTerm} returned {ResultCount} of {TotalCount} results in {ElapsedMs:0} ms",
            term,
            items.Count,
            response?.NumFound ?? 0,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

        return new CatalogSearchPage(items, response?.NumFound ?? 0);
    }

    /// <summary>ISBN-looking input is searched by ISBN for an exact hit; anything else is a general query.</summary>
    internal static string BuildSearchUri(string term, int page, int pageSize)
    {
        var criteria = TryGetIsbn(term) is { } isbn ? $"isbn={isbn}" : $"q={Uri.EscapeDataString(term)}";

        return $"search.json?{criteria}&fields={SearchFields}&page={page}&limit={pageSize}";
    }

    private static string? TryGetIsbn(string term)
    {
        var isbn = Book.NormalizeIsbn(term);
        var looksLikeIsbn = isbn is { Length: 10 or 13 } && term.All(c => char.IsDigit(c) || c is '-' or ' ' or 'X' or 'x');
        return looksLikeIsbn ? isbn : null;
    }

    private CatalogBook? Normalize(OpenLibraryDoc doc, string? searchedIsbn)
    {
        if (string.IsNullOrWhiteSpace(doc.Key) || string.IsNullOrWhiteSpace(doc.Title))
        {
            return null;
        }

        var authors = doc.AuthorName?.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList() ?? [];

        // A work lists many editions' ISBNs: keep the one the user searched for, otherwise the first valid one.
        var isbns = doc.Isbn?.Select(Book.NormalizeIsbn).Where(i => i is { Length: 10 or 13 }).ToList() ?? [];
        var isbn = searchedIsbn is not null && isbns.Contains(searchedIsbn) ? searchedIsbn : isbns.FirstOrDefault();

        return new CatalogBook(
            OpenLibraryKey: doc.Key,
            Title: doc.Title.Trim(),
            Author: authors.Count > 0 ? string.Join(", ", authors) : "Unknown author",
            Isbn: isbn,
            PublishedYear: doc.FirstPublishYear,
            CoverUrl: BuildCoverUrl(doc.CoverId, isbn));
    }

    /// <summary>
    /// Prefer the cover id; fall back to the ISBN-based cover; otherwise no cover.
    /// <c>default=false</c> makes Open Library return 404 (instead of a blank 1x1 image) when it has
    /// no cover for that ISBN, so the client can show its own placeholder.
    /// </summary>
    private string? BuildCoverUrl(long? coverId, string? isbn)
    {
        var coversBase = options.Value.CoversBaseUrl.TrimEnd('/');
        if (coverId is > 0)
        {
            return $"{coversBase}/b/id/{coverId}-M.jpg";
        }

        return isbn is null ? null : $"{coversBase}/b/isbn/{isbn}-M.jpg?default=false";
    }

    private static bool IsTransientFailure(Exception ex, CancellationToken cancellationToken) => ex switch
    {
        HttpRequestException or JsonException or TimeoutRejectedException or BrokenCircuitException => true,
        // HttpClient's own timeout surfaces as TaskCanceledException without the caller having cancelled.
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false,
    };
}
