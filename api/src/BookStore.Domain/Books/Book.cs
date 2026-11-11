using BookStore.Domain.Common;

namespace BookStore.Domain.Books;

/// <summary>
/// A book owned by the store. Catalog fields are captured once from Open Library when the
/// book is added and are immutable afterwards; store fields (genre, price, stock, description)
/// are the only things that can change.
/// </summary>
public sealed class Book
{
    public const int TitleMaxLength = 500;
    public const int AuthorMaxLength = 500;
    public const int IsbnMaxLength = 13;
    public const int OpenLibraryKeyMaxLength = 64;
    public const int CoverUrlMaxLength = 512;
    public const int DescriptionMaxLength = 4000;
    public const decimal MaxPrice = 100_000m;
    public const int MaxStock = 1_000_000;

    // Required by EF Core for materialisation.
    private Book()
    {
    }

    public Guid Id { get; private set; }

    // --- Captured from Open Library at add-time ---
    public string Title { get; private set; } = null!;
    public string Author { get; private set; } = null!;
    public string? Isbn { get; private set; }
    public int? PublishedYear { get; private set; }
    public string? CoverUrl { get; private set; }
    public string OpenLibraryKey { get; private set; } = null!;

    // --- Owned by the store ---
    public string? Description { get; private set; }
    public Genre Genre { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    public DateTime AddedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public static Book Create(
        string title,
        string author,
        string? isbn,
        int? publishedYear,
        string? coverUrl,
        string openLibraryKey,
        string? description,
        Genre genre,
        decimal price,
        int stock,
        DateTime addedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("A book must have a title.");
        }

        if (string.IsNullOrWhiteSpace(openLibraryKey))
        {
            throw new DomainException("A book must reference the Open Library work it came from.");
        }

        var book = new Book
        {
            Id = Guid.CreateVersion7(),
            Title = title.Trim(),
            Author = string.IsNullOrWhiteSpace(author) ? "Unknown author" : author.Trim(),
            Isbn = NormalizeIsbn(isbn),
            PublishedYear = publishedYear,
            CoverUrl = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl.Trim(),
            OpenLibraryKey = openLibraryKey.Trim(),
            AddedAtUtc = addedAtUtc,
        };

        book.ApplyStoreDetails(genre, price, stock, description);
        return book;
    }

    public void UpdateStoreDetails(Genre genre, decimal price, int stock, string? description, DateTime updatedAtUtc)
    {
        ApplyStoreDetails(genre, price, stock, description);
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Strips hyphens/spaces so "978-0-441-17271-9" and "9780441172719" compare equal.</summary>
    public static string? NormalizeIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        var normalized = new string(isbn.Where(c => char.IsDigit(c) || c is 'X' or 'x').ToArray()).ToUpperInvariant();
        return normalized.Length == 0 ? null : normalized;
    }

    private void ApplyStoreDetails(Genre genre, decimal price, int stock, string? description)
    {
        if (!Enum.IsDefined(genre))
        {
            throw new DomainException($"'{genre}' is not a known genre.");
        }

        if (price is < 0 or > MaxPrice)
        {
            throw new DomainException($"Price must be between 0 and {MaxPrice}.");
        }

        if (stock is < 0 or > MaxStock)
        {
            throw new DomainException($"Stock must be between 0 and {MaxStock}.");
        }

        Genre = genre;
        Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        Stock = stock;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
