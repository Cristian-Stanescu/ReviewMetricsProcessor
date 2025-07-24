namespace ReviewMetricsProcessor.Messages;

public record ReviewStarted(
    string ReviewId,
    string AuthorId,
    DateTime Timestamp
);