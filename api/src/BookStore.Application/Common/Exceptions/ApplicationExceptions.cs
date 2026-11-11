namespace BookStore.Application.Common.Exceptions;

// Application-level failures. The API layer translates each one into an RFC 7807 ProblemDetails
// response, so handlers can express "what went wrong" without knowing about HTTP.

public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.");

public sealed class ConflictException(string message) : Exception(message);

public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

/// <summary>The external book catalog (Open Library) failed, timed out or returned garbage.</summary>
public sealed class CatalogUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
