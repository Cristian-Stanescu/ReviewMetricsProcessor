using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ReviewMetricsProcessor.Messages;

namespace ReviewMetricsProcessor.Endpoints.Reviews;

public class ProcessReviewEventsBatch
{
    public static async Task<Results<Ok, BadRequest<string>>> Handle(
        [FromBody] List<ReviewEvent> reviewEvents,
        [FromServices] IBus bus,
        [FromServices] IValidator<ReviewEvent> validator,
        ILogger<ProcessReviewEventsBatch> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Received {Count} review events for processing", reviewEvents.Count);

        try
        {
            var invalidEvents = new List<ReviewEvent>();
            foreach (var reviewEvent in reviewEvents)
            {
                var validationResult = await validator.ValidateAsync(reviewEvent, ct);
                if (!validationResult.IsValid)
                {
                    logger.LogWarning("Invalid review event: {Event}", reviewEvent);
                    invalidEvents.Add(reviewEvent);
                }
            }

            foreach (var reviewEvent in reviewEvents.Except(invalidEvents))
            {
                switch (reviewEvent.Type)
                {
                    case "ReviewStarted":
                        await bus.Publish(new ReviewStarted(
                            reviewEvent.ReviewId,
                            reviewEvent.AuthorId,
                            reviewEvent.Timestamp), ct);
                        break;
                    case "ReviewCompleted":
                        await bus.Publish(new ReviewCompleted(
                            reviewEvent.ReviewId,
                            reviewEvent.AuthorId,
                            reviewEvent.Timestamp,
                            reviewEvent.LinesOfCodeReviewed!.Value), ct);
                        break;
                    default:
                        logger.LogWarning("Invalid review event type: {Type}", reviewEvent.Type);
                        return TypedResults.BadRequest("Invalid review event type.");
                }
            }

            logger.LogInformation("Successfully queued {Count} review events for processing", reviewEvents.Count);
            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing review events.");
            return TypedResults.BadRequest("Error processing review events.");
        }
    }
}

public record ReviewEvent(string Type, string ReviewId, string AuthorId, DateTime Timestamp, int? LinesOfCodeReviewed);
