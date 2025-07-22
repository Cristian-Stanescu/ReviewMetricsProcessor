namespace ReviewMetricsProcessor.Data.Entities;

public class Author
{
    public string Id { get; set; } = string.Empty;
    public List<Review> Reviews { get; set; } = [];
    public int CompletedReviews => Reviews.Count(r => r.CompletedTimestamp is not null);
    public int TotalReviews => Reviews.Count;
    public double TotalLinesOfCodeReviewed { get; set; }
    public double LinesOfCodeReviewedPerHour { get; set; }
    public double? AverageReviewDuration { get; set; }

    public void UpdateMetrics()
    {
        var completedReviews = Reviews.Where(r => r.CompletedTimestamp is not null).ToList();
        TotalLinesOfCodeReviewed = completedReviews.Sum(r => r.LinesOfCode ?? 0);

        if (completedReviews.Count > 0)
        {
            var totalDurationSeconds = completedReviews
                .Sum(r => (r.CompletedTimestamp!.Value - r.StartedTimestamp).TotalSeconds);
            LinesOfCodeReviewedPerHour = totalDurationSeconds > 0
                ? TotalLinesOfCodeReviewed / (totalDurationSeconds / 3600)
                : 0;
            AverageReviewDuration = totalDurationSeconds / completedReviews.Count;
        }
        else
        {
            LinesOfCodeReviewedPerHour = 0;
            AverageReviewDuration = null;
        }
    }
}
