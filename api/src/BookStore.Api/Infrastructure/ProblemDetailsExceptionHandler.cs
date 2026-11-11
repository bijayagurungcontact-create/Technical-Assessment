using System.Text.Json;
using BookStore.Application.Common.Exceptions;
using BookStore.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Infrastructure;

/// <summary>Maps application/domain exceptions to RFC 7807 ProblemDetails responses.</summary>
internal sealed class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            RequestValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(e => JsonNamingPolicy.CamelCase.ConvertName(e.Key), e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
            },
            DomainException domain => Problem(StatusCodes.Status400BadRequest, "Invalid request", domain.Message),
            BadHttpRequestException badRequest => Problem(badRequest.StatusCode, "Malformed request", badRequest.Message),
            NotFoundException notFound => Problem(StatusCodes.Status404NotFound, "Not found", notFound.Message),
            ConflictException conflict => Problem(StatusCodes.Status409Conflict, "Duplicate book", conflict.Message),
            CatalogUnavailableException unavailable => Problem(StatusCodes.Status503ServiceUnavailable, "Catalog unavailable", unavailable.Message),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected error", "Something went wrong on our side."),
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError && exception is not CatalogUnavailableException)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        problem.Extensions["correlationId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
