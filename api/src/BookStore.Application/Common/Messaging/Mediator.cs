using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Application.Common.Messaging;

/// <summary>
/// Minimal in-process mediator: resolves the single handler for a request and wraps it in the
/// registered pipeline behaviors (first registered = outermost). Hand-rolled on purpose: the
/// feature set we need is ~50 lines, and it avoids MediatR's commercial licence.
/// </summary>
internal sealed class Mediator(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, object> Dispatchers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Reflection runs once per request type; afterwards dispatch is a dictionary lookup + virtual call.
        var dispatcher = (Dispatcher<TResponse>)Dispatchers.GetOrAdd(
            request.GetType(),
            requestType => Activator.CreateInstance(
                typeof(Dispatcher<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return dispatcher.Dispatch(request, serviceProvider, cancellationToken);
    }

    private abstract class Dispatcher<TResponse>
    {
        public abstract Task<TResponse> Dispatch(object request, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class Dispatcher<TRequest, TResponse> : Dispatcher<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Dispatch(object request, IServiceProvider services, CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(typedRequest, cancellationToken);

            foreach (var behavior in services.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse())
            {
                var next = pipeline;
                pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
            }

            return pipeline();
        }
    }
}
