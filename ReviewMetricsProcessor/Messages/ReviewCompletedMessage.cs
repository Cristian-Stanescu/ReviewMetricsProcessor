namespace ReviewMetricsProcessor.Messages;

public record ReviewCompletedMessage(
    string ReviewId,
    string AuthorId,
    DateTime Timestamp,
    int LinesOfCodeReviewed
);