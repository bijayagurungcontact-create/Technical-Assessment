using System.Diagnostics;
using BookStore.Application.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace BookStore.Application.Common.Behaviors;

/// <summary>Logs every request with its name and duration as structured properties.</summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var response = await next();
            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs:0.0} ms",
                requestName,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "{RequestName} failed after {ElapsedMs:0.0} ms with {ExceptionType}",
                requestName,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                ex.GetType().Name);
            throw;
        }
    }
}
