using MassTransit;
using Microsoft.EntityFrameworkCore;
using ReviewMetricsProcessor.Data;
using ReviewMetricsProcessor.Messages;

namespace ReviewMetricsProcessor.Consumers;

public class ReviewCompletedConsumer(ReviewMetricsDbContext dbContext, ILogger<ReviewCompletedConsumer> logger) : IConsumer<ReviewCompletedMessage>
{
    public async Task Consume(ConsumeContext<ReviewCompletedMessage> context)
    {
        var message = context.Message;
        logger.LogInformation("Processing ReviewCompleted for ReviewId: {ReviewId}, AuthorId: {AuthorId}",
            message.ReviewId, message.AuthorId);

        try
        {
            var existingAuthor = await dbContext.Authors
                .Include(a => a.Reviews)
                .FirstOrDefaultAsync(a => a.Id == message.AuthorId, context.CancellationToken);

            var existingReview = existingAuthor?.Reviews
                .FirstOrDefault(r => r.Id == message.ReviewId);

            if (existingAuthor is null)
            {
                throw new InvalidOperationException($"Author with ID {message.AuthorId} not found.");
            }

            if (existingReview is null)
            {
                throw new InvalidOperationException($"Review with ID {message.ReviewId} and Author ID {message.AuthorId} not found.");
            }

            if (existingReview.CompletedTimestamp.HasValue)
            {
                throw new InvalidOperationException($"Review with ID {message.ReviewId} and Author ID {message.AuthorId} has already been completed.");
            }

            existingReview.CompletedTimestamp = message.Timestamp;
            existingReview.LinesOfCode = message.LinesOfCodeReviewed;
            existingAuthor.UpdateMetrics();
            await dbContext.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation("Successfully processed ReviewCompleted for ReviewId: {ReviewId}, AuthorId: {AuthorId}",
                message.ReviewId, message.AuthorId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ReviewCompleted for ReviewId: {ReviewId}, AuthorId: {AuthorId}",
                message.ReviewId, message.AuthorId);
            throw;
        }
    }
}