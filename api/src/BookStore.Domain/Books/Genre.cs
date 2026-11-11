namespace BookStore.Domain.Books;

/// <summary>
/// Controlled vocabulary for the store's genre filter. Open Library subjects are
/// free-form, so the genre is always chosen by the store, never imported.
/// </summary>
public enum Genre
{
    Fiction,
    SciFi,
    Fantasy,
    Mystery,
    Biography,
    NonFiction,
    Children,
    History,
}
