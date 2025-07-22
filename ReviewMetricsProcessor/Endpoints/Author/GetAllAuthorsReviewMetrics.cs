using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ReviewMetricsProcessor.Data;

namespace ReviewMetricsProcessor.Endpoints.Author;

public class GetAllAuthorsReviewMetrics
{
    public static async Task<Ok<List<AuthorMetricsResponse>>> Handle(
        ReviewMetricsDbContext context, CancellationToken ct)
    {
        var response = await context.Authors
            .Include(a => a.Reviews)
            .Select(author => new AuthorMetricsResponse(
                author.Id,
                author.CompletedReviews,
                author.TotalReviews,
                author.TotalLinesOfCodeReviewed,
                author.LinesOfCodeReviewedPerHour,
                author.AverageReviewDuration))
            .ToListAsync(ct);

        return TypedResults.Ok(response);
    }
}