using FluentValidation;

namespace BookStore.Application.Features.Books.GetBooks;

internal sealed class GetBooksValidator : AbstractValidator<GetBooksQuery>
{
    public GetBooksValidator()
    {
        RuleFor(x => x.Genre).IsInEnum().When(x => x.Genre.HasValue);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, GetBooksQuery.MaxPageSize);

        RuleFor(x => x.SortBy)
            .Must(BookSorting.IsKnownField)
            .WithMessage($"SortBy must be one of: {string.Join(", ", BookSorting.Fields)}.");

        RuleFor(x => x.SortDirection)
            .Must(BookSorting.IsKnownDirection)
            .WithMessage("SortDirection must be 'asc' or 'desc'.");
    }
}
