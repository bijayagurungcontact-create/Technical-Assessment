namespace BookStore.Application.Common.Messaging;

/// <summary>A message with a single handler that produces <typeparamref name="TResponse"/>.</summary>
public interface IRequest<out TResponse>;

/// <summary>A request that changes state. Commands are the only requests allowed to write.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>A side-effect-free request that only reads state.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>Cross-cutting step wrapped around every handler (validation, logging, ...).</summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>Entry point used by the API layer: dispatches a request to its handler.</summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>Return type for commands that have nothing meaningful to return.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
