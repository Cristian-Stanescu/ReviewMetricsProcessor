using ReviewMetricsProcessor.Endpoints.Author;
using ReviewMetricsProcessor.Endpoints.Reviews;

namespace ReviewMetricsProcessor.Endpoints;

public static class EndpointsRegistrations
{
    public static WebApplication UseReviewMetricsEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api")
            .WithOpenApi();

        var reviewsGroup = apiGroup.MapGroup("/reviews")
            .WithTags("Reviews");
        reviewsGroup.MapPost("/", ProcessReviewEventsBatch.Handle);

        var authorsGroup = apiGroup.MapGroup("/authors")
            .WithTags("Authors");
        authorsGroup.MapGet("/", GetAllAuthorsReviewMetrics.Handle);
        authorsGroup.MapGet("/{authorId}/", GetAuthorReviewMetrics.Handle);

        return app;
    }
}
