using System.Text.RegularExpressions;
using Serilog.Context;

namespace BookStore.Api.Infrastructure;

/// <summary>
/// Gives every request a correlation id (taken from <c>X-Correlation-ID</c> when the caller sends a
/// sane one, generated otherwise), echoes it back, and pushes it into the Serilog log context so
/// every log line of the request — including the outbound Open Library call — carries it.
/// </summary>
internal sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault() is { } incoming && SafeId().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    // Don't let callers inject arbitrary content into logs/headers.
    [GeneratedRegex("^[A-Za-z0-9-_.]{1,64}$")]
    private static partial Regex SafeId();
}
