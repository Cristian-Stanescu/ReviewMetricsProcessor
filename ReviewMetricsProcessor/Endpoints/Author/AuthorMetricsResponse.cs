namespace ReviewMetricsProcessor.Endpoints.Author;

public record AuthorMetricsResponse(
    string AuthorId,
    int CompletedReviews,
    int TotalReviews,
    double TotalLinesOfCodeReviewed,
    double LinesOfCodeReviewedPerHour,
    double? AverageReviewDuration);