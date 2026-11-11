using System.Text.Json.Serialization;
using BookStore.Api.Endpoints;
using BookStore.Api.Infrastructure;
using BookStore.Application;
using BookStore.Infrastructure;
using BookStore.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
    builder.Services.AddOpenApi();

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapBookEndpoints();
    app.MapCatalogEndpoints();

    await app.Services.InitializeDatabaseAsync();
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "BookStore API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
