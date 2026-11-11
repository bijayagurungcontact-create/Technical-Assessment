using FluentValidation;

namespace BookStore.Application.Features.Books.UpdateBook;

internal sealed class UpdateBookValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.Genre).ValidGenre();
        RuleFor(x => x.Price).ValidPrice();
        RuleFor(x => x.Stock).ValidStock();
        RuleFor(x => x.Description).ValidDescription();
    }
}
