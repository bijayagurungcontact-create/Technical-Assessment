using BookStore.Domain.Books;
using FluentValidation;

namespace BookStore.Application.Features.Books;

/// <summary>Validation for the store-owned fields, shared by AddBook and UpdateBook.</summary>
internal static class StoreDetailsRules
{
    public static IRuleBuilderOptions<T, Genre> ValidGenre<T>(this IRuleBuilder<T, Genre> rule) =>
        rule.IsInEnum().WithMessage("Genre must be one of: " + string.Join(", ", Enum.GetNames<Genre>()) + ".");

    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.InclusiveBetween(0, Book.MaxPrice)
            .PrecisionScale(8, 2, ignoreTrailingZeros: true)
            .WithMessage("Price can have at most 2 decimal places.");

    public static IRuleBuilderOptions<T, int> ValidStock<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(0, Book.MaxStock);

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(Book.DescriptionMaxLength);
}
