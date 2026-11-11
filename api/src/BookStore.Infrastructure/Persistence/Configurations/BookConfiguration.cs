using BookStore.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Infrastructure.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Title).IsRequired().HasMaxLength(Book.TitleMaxLength);
        builder.Property(b => b.Author).IsRequired().HasMaxLength(Book.AuthorMaxLength);
        builder.Property(b => b.Isbn).HasMaxLength(Book.IsbnMaxLength);
        builder.Property(b => b.CoverUrl).HasMaxLength(Book.CoverUrlMaxLength);
        builder.Property(b => b.OpenLibraryKey).IsRequired().HasMaxLength(Book.OpenLibraryKeyMaxLength);
        builder.Property(b => b.Description).HasMaxLength(Book.DescriptionMaxLength);

        // Stored as text so the data is readable and the raw SQL can group on it directly.
        builder.Property(b => b.Genre).IsRequired().HasConversion<string>().HasMaxLength(32);

        // SQLite has no decimal type and EF cannot ORDER BY a decimal stored as TEXT, so money is
        // stored as integer cents: exact, and sortable server-side.
        builder.Property(b => b.Price)
            .HasConversion(price => (long)(price * 100m), cents => cents / 100m)
            .HasColumnName("PriceCents");

        // SQLite stores DateTime as text without a kind; re-tag on read so the API emits "...Z".
        builder.Property(b => b.AddedAtUtc)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        builder.Property(b => b.UpdatedAtUtc)
            .HasConversion(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        // Duplicate prevention at the database level (the handler checks first for a nicer message).
        builder.HasIndex(b => b.OpenLibraryKey).IsUnique();
        builder.HasIndex(b => b.Isbn).IsUnique().HasFilter("\"Isbn\" IS NOT NULL");

        // Supports the genre filter + the GROUP BY used by the genre statistics query.
        builder.HasIndex(b => b.Genre);
    }
}
