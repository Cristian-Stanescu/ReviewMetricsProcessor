using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ReviewMetricsProcessor.Data;

namespace ReviewMetricsProcessor.Endpoints.Author;

public class GetAuthorReviewMetrics
{
    public static async Task<Results<Ok<AuthorMetricsResponse>, NotFound>> Handle(
        string authorId,
        ReviewMetricsDbContext context, CancellationToken ct)
    {
        var author = await context.Authors
            .Include(a => a.Reviews)
            .FirstAsync(r => r.Id == authorId, ct);

        var response = new AuthorMetricsResponse(
            author.Id,
            author.CompletedReviews,
            author.TotalReviews,
            author.TotalLinesOfCodeReviewed,
            author.LinesOfCodeReviewedPerHour,
            author.AverageReviewDuration);

        return TypedResults.Ok(response);
    }
}
