using BookStore.Domain.Books;
using FluentValidation;

namespace BookStore.Application.Features.Books.AddBook;

internal sealed class AddBookValidator : AbstractValidator<AddBookCommand>
{
    // Covers are only ever derived from the Open Library Covers API, so anything else is rejected
    // rather than letting a client store an arbitrary URL that the web app would then render.
    private const string CoversHost = "covers.openlibrary.org";

    public AddBookValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.OpenLibraryKey)
            .NotEmpty()
            .MaximumLength(Book.OpenLibraryKeyMaxLength)
            .Matches(@"^/(works|books)/OL\d+[WM]$")
            .WithMessage("OpenLibraryKey must look like '/works/OL45804W'.");

        RuleFor(x => x.Title).NotEmpty().MaximumLength(Book.TitleMaxLength);
        RuleFor(x => x.Author).NotEmpty().MaximumLength(Book.AuthorMaxLength);

        RuleFor(x => Book.NormalizeIsbn(x.Isbn))
            .Must(isbn => isbn is null || isbn.Length is 10 or 13)
            .OverridePropertyName(nameof(AddBookCommand.Isbn))
            .WithMessage("ISBN must have 10 or 13 characters.");

        RuleFor(x => x.PublishedYear)
            .InclusiveBetween(0, timeProvider.GetUtcNow().Year + 1)
            .When(x => x.PublishedYear.HasValue);

        RuleFor(x => x.CoverUrl)
            .MaximumLength(Book.CoverUrlMaxLength)
            .Must(BeAnOpenLibraryCoverUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.CoverUrl))
            .WithMessage($"CoverUrl must be an https URL on {CoversHost}.");

        RuleFor(x => x.Description).ValidDescription();
        RuleFor(x => x.Genre).ValidGenre();
        RuleFor(x => x.Price).ValidPrice();
        RuleFor(x => x.Stock).ValidStock();
    }

    private static bool BeAnOpenLibraryCoverUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host.Equals(CoversHost, StringComparison.OrdinalIgnoreCase);
}
