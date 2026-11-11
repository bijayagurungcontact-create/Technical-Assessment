using System.Text.Json.Serialization;

namespace BookStore.Infrastructure.Catalog.OpenLibrary;

// Wire format of https://openlibrary.org/search.json — only the fields we request via `fields=`.

internal sealed record OpenLibrarySearchResponse(
    [property: JsonPropertyName("numFound")] int NumFound,
    [property: JsonPropertyName("docs")] IReadOnlyList<OpenLibraryDoc>? Docs);

internal sealed record OpenLibraryDoc(
    [property: JsonPropertyName("key")] string? Key,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("author_name")] IReadOnlyList<string>? AuthorName,
    [property: JsonPropertyName("first_publish_year")] int? FirstPublishYear,
    [property: JsonPropertyName("isbn")] IReadOnlyList<string>? Isbn,
    [property: JsonPropertyName("cover_i")] long? CoverId);
