namespace ReviewMetricsProcessor.Messages;

public record ReviewStartedMessage(
    string ReviewId,
    string AuthorId,
    DateTime Timestamp
);