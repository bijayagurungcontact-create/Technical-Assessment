using BookStore.Application.Common.Messaging;
using BookStore.Application.Features.Catalog.SearchCatalog;

namespace BookStore.Api.Endpoints;

/// <summary>Server-side proxy to Open Library. The browser never calls Open Library directly.</summary>
internal static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/catalog/search", SearchCatalog)
            .WithTags("Catalog")
            .WithName(nameof(SearchCatalog));

        return app;
    }

    private static Task<CatalogSearchResponse> SearchCatalog(
        ISender sender,
        CancellationToken ct,
        string? q = null,
        int page = 1,
        int pageSize = SearchCatalogQuery.DefaultPageSize) =>
        sender.Send(new SearchCatalogQuery(q ?? string.Empty, page, pageSize), ct);
}
