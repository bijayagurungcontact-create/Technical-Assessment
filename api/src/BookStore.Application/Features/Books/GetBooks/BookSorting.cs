using System.Linq.Expressions;
using BookStore.Domain.Books;

namespace BookStore.Application.Features.Books.GetBooks;

/// <summary>Whitelist of sortable columns, so clients cannot sort by arbitrary expressions.</summary>
internal static class BookSorting
{
    private static readonly Dictionary<string, Expression<Func<Book, object?>>> SortKeys =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = b => b.Title,
            ["author"] = b => b.Author,
            ["publishedYear"] = b => b.PublishedYear,
            ["genre"] = b => b.Genre,
            ["price"] = b => b.Price,
            ["stock"] = b => b.Stock,
            ["addedAt"] = b => b.AddedAtUtc,
        };

    public static IEnumerable<string> Fields => SortKeys.Keys;

    public static bool IsKnownField(string? field) => field is null || SortKeys.ContainsKey(field);

    public static bool IsKnownDirection(string? direction) =>
        direction is null
        || direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
        || direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

    /// <summary>Defaults to newest first; ties are broken by Id so paging is stable.</summary>
    public static IOrderedQueryable<Book> Apply(IQueryable<Book> books, string? field, string? direction)
    {
        var key = SortKeys[field ?? "addedAt"];
        var descending = direction is null
            ? field is null
            : direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return descending
            ? books.OrderByDescending(key).ThenBy(b => b.Id)
            : books.OrderBy(key).ThenBy(b => b.Id);
    }
}
