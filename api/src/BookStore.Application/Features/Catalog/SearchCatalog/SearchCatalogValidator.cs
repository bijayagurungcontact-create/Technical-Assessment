using FluentValidation;

namespace BookStore.Application.Features.Catalog.SearchCatalog;

internal sealed class SearchCatalogValidator : AbstractValidator<SearchCatalogQuery>
{
    public SearchCatalogValidator()
    {
        RuleFor(x => x.Term)
            .Must(term => !string.IsNullOrWhiteSpace(term) && term.Trim().Length >= 2)
            .WithMessage("Search term must be at least 2 characters.")
            .MaximumLength(200);

        RuleFor(x => x.Page).InclusiveBetween(1, 100);
        RuleFor(x => x.PageSize).InclusiveBetween(1, SearchCatalogQuery.MaxPageSize);
    }
}
