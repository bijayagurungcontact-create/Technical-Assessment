using BookStore.Application.Common.Behaviors;
using BookStore.Application.Common.Exceptions;
using BookStore.Application.Common.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.UnitTests.Application;

public sealed class MediatorTests
{
    [Fact]
    public async Task Behaviors_wrap_the_handler_in_registration_order()
    {
        var calls = new List<string>();
        await using var services = new ServiceCollection()
            .AddSingleton(calls)
            .AddScoped<ISender, Mediator>()
            .AddScoped<IRequestHandler<Ping, string>, PingHandler>()
            .AddScoped<IPipelineBehavior<Ping, string>, OuterBehavior>()
            .AddScoped<IPipelineBehavior<Ping, string>, InnerBehavior>()
            .BuildServiceProvider();

        var result = await services.GetRequiredService<ISender>().Send(new Ping("hi"));

        Assert.Equal("pong: hi", result);
        Assert.Equal(["outer:before", "inner:before", "handler", "inner:after", "outer:after"], calls);
    }

    [Fact]
    public async Task Validation_behavior_stops_invalid_requests_before_the_handler()
    {
        var calls = new List<string>();
        await using var services = new ServiceCollection()
            .AddSingleton(calls)
            .AddScoped<ISender, Mediator>()
            .AddScoped<IRequestHandler<Ping, string>, PingHandler>()
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))
            .AddScoped<IValidator<Ping>, PingValidator>()
            .BuildServiceProvider();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => services.GetRequiredService<ISender>().Send(new Ping("")));

        Assert.Contains(nameof(Ping.Message), ex.Errors.Keys);
        Assert.Empty(calls);
    }

    public sealed record Ping(string Message) : IQuery<string>;

    private sealed class PingHandler(List<string> calls) : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            calls.Add("handler");
            return Task.FromResult($"pong: {request.Message}");
        }
    }

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(p => p.Message).NotEmpty();
    }

    private abstract class RecordingBehavior(List<string> calls, string name) : IPipelineBehavior<Ping, string>
    {
        public async Task<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            calls.Add($"{name}:before");
            var response = await next();
            calls.Add($"{name}:after");
            return response;
        }
    }

    private sealed class OuterBehavior(List<string> calls) : RecordingBehavior(calls, "outer");

    private sealed class InnerBehavior(List<string> calls) : RecordingBehavior(calls, "inner");
}
