namespace ReviewMetricsProcessor.Messages;

public record ReviewCompleted(
    string ReviewId,
    string AuthorId,
    DateTime Timestamp,
    int LinesOfCodeReviewed
);