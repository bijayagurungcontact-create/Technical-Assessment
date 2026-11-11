namespace BookStore.Domain.Common;

/// <summary>Raised when an operation would leave an entity in an invalid state.</summary>
public sealed class DomainException(string message) : Exception(message);
