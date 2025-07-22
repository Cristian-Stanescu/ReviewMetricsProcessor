using MassTransit;
using Microsoft.EntityFrameworkCore;
using ReviewMetricsProcessor.Data;
using ReviewMetricsProcessor.Data.Entities;
using ReviewMetricsProcessor.Messages;

namespace ReviewMetricsProcessor.Consumers;

public class ReviewStartedConsumer(ReviewMetricsDbContext dbContext, ILogger<ReviewStartedConsumer> logger) : IConsumer<ReviewStartedMessage>
{
    public async Task Consume(ConsumeContext<ReviewStartedMessage> context)
    {
        var message = context.Message;
        logger.LogInformation("Processing ReviewStarted for ReviewId: {ReviewId}, AuthorId: {AuthorId}", 
            message.ReviewId, message.AuthorId);

        try
        {
            var existingAuthor = await dbContext.Authors
                .Include(a => a.Reviews)
                .FirstOrDefaultAsync(a => a.Id == message.AuthorId, context.CancellationToken);

            if (existingAuthor != null)
            {
                var existingReview = existingAuthor.Reviews.FirstOrDefault(r => r.Id == message.ReviewId);
                if (existingReview != null)
                {
                    existingReview.StartedTimestamp = message.Timestamp;
                    await dbContext.SaveChangesAsync(context.CancellationToken);
                }
                else
                {
                    var newReview = new Review
                    {
                        Id = message.ReviewId,
                        StartedTimestamp = message.Timestamp
                    };
                    existingAuthor.Reviews.Add(newReview);
                    await dbContext.SaveChangesAsync(context.CancellationToken);
                }
            }
            else
            {
                var newAuthor = new Author
                {
                    Id = message.AuthorId,
                    Reviews = [
                        new Review
                        {
                            Id = message.ReviewId,
                            StartedTimestamp = message.Timestamp
                        }
                    ]
                };
                dbContext.Authors.Add(newAuthor);
                await dbContext.SaveChangesAsync(context.CancellationToken);
            }

            logger.LogInformation("Successfully processed ReviewStarted for ReviewId: {ReviewId}, AuthorId: {AuthorId}", 
                message.ReviewId, message.AuthorId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing ReviewStarted for ReviewId: {ReviewId}, AuthorId: {AuthorId}", 
                message.ReviewId, message.AuthorId);
            throw;
        }
    }
}