using System.Net;
using System.Text;
using BookStore.Application.Common.Exceptions;
using BookStore.Infrastructure.Catalog.OpenLibrary;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookStore.UnitTests.Infrastructure;

public sealed class OpenLibraryBookCatalogTests
{
    [Fact]
    public async Task Normalizes_search_results()
    {
        const string json =
            """
            {
              "numFound": 42,
              "docs": [
                { "key": "/works/OL893414W", "title": "Dune", "author_name": ["Frank Herbert", "Frank Herbert"],
                  "first_publish_year": 1965, "isbn": ["044117271X", "9780441172719"], "cover_i": 11481354 },
                { "key": "/works/OL2W", "title": "No cover id", "author_name": ["A", "B"], "isbn": ["123", "9780000000002"] },
                { "key": "/works/OL3W", "title": "Bare" },
                { "key": "/works/OL4W" }
              ]
            }
            """;
        var catalog = CreateCatalog(_ => Json(json));

        var page = await catalog.SearchAsync("dune", 1, 10, CancellationToken.None);

        Assert.Equal(42, page.TotalCount);
        Assert.Equal(3, page.Items.Count); // the doc without a title is dropped

        var dune = page.Items[0];
        Assert.Equal("Frank Herbert", dune.Author);
        Assert.Equal("044117271X", dune.Isbn);
        Assert.Equal(1965, dune.PublishedYear);
        Assert.Equal("https://covers.openlibrary.org/b/id/11481354-M.jpg", dune.CoverUrl);

        var noCoverId = page.Items[1];
        Assert.Equal("A, B", noCoverId.Author);
        Assert.Equal("9780000000002", noCoverId.Isbn); // invalid "123" skipped
        Assert.Equal("https://covers.openlibrary.org/b/isbn/9780000000002-M.jpg?default=false", noCoverId.CoverUrl);

        var bare = page.Items[2];
        Assert.Equal("Unknown author", bare.Author);
        Assert.Null(bare.Isbn);
        Assert.Null(bare.CoverUrl);
    }

    [Fact]
    public async Task Isbn_search_keeps_the_isbn_the_user_searched_for()
    {
        const string json =
            """
            { "numFound": 1, "docs": [ { "key": "/works/OL27258W", "title": "Neuromancer", "isbn": ["8576571919", "9780441569595"] } ] }
            """;
        var catalog = CreateCatalog(_ => Json(json));

        var page = await catalog.SearchAsync("978-0-441-56959-5", 1, 10, CancellationToken.None);

        Assert.Equal("9780441569595", page.Items.Single().Isbn);
    }

    [Theory]
    [InlineData("the lord of the rings", "search.json?q=the%20lord%20of%20the%20rings&")]
    [InlineData("978-0-441-17271-9", "search.json?isbn=9780441172719&")]
    public void Builds_a_query_or_isbn_search(string term, string expectedPrefix) =>
        Assert.StartsWith(expectedPrefix, OpenLibraryBookCatalog.BuildSearchUri(term, 1, 10));

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    [InlineData(HttpStatusCode.OK, "<html>not json</html>")]
    public async Task Upstream_failures_become_CatalogUnavailableException(HttpStatusCode status, string body)
    {
        var catalog = CreateCatalog(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });

        await Assert.ThrowsAsync<CatalogUnavailableException>(
            () => catalog.SearchAsync("dune", 1, 10, CancellationToken.None));
    }

    [Fact]
    public async Task Timeouts_become_CatalogUnavailableException()
    {
        var catalog = CreateCatalog(_ => throw new TaskCanceledException("HttpClient timeout"));

        await Assert.ThrowsAsync<CatalogUnavailableException>(
            () => catalog.SearchAsync("dune", 1, 10, CancellationToken.None));
    }

    private static OpenLibraryBookCatalog CreateCatalog(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(
            new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("https://openlibrary.org/") },
            Options.Create(new OpenLibraryOptions()),
            NullLogger<OpenLibraryBookCatalog>.Instance);

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
