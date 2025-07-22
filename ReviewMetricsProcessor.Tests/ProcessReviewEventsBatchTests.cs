using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using ReviewMetricsProcessor.Endpoints.Reviews;
using ReviewMetricsProcessor.Validators;

namespace ReviewMetricsProcessor.Tests;

public class ProcessReviewEventsBatchTests
{
    private readonly IBus _bus = Substitute.For<IBus>();
    private readonly IValidator<ReviewEvent> _validator = new ReviewEventValidator();
    private readonly ILogger<ProcessReviewEventsBatch> _logger = Substitute.For<ILogger<ProcessReviewEventsBatch>>();

    [Fact]
    public async Task Handle_WithValidEvents_ReturnsOk()
    {
        // Arrange
        var reviewEvents = new List<ReviewEvent>
        {
            new("ReviewStarted", "review1", "author1", DateTime.UtcNow, null),
            new("ReviewCompleted", "review2", "author2", DateTime.UtcNow, 150)
        };

        // Act
        var result = await ProcessReviewEventsBatch.Handle(
            reviewEvents, _bus, _validator, _logger, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<Ok>();
    }

    [Fact]
    public async Task Handle_WithInvalidEventType_ReturnsBadRequest()
    {
        // Arrange
        var reviewEvents = new List<ReviewEvent>
        {
            new("InvalidType", "review1", "author1", DateTime.UtcNow, 100)
        };

        // Act
        var result = await ProcessReviewEventsBatch.Handle(
            reviewEvents, _bus, _validator, _logger, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<BadRequest<string>>();

        var badRequestResult = result.Result as BadRequest<string>;
        badRequestResult!.Value.Should().Be("Invalid review event type.");
    }

    [Fact]
    public async Task Handle_WithReviewCompletedMissingLinesOfCode_ReturnsBadRequest()
    {
        // Arrange
        var reviewEvents = new List<ReviewEvent>
        {
            new("ReviewCompleted", "review1", "author1", DateTime.UtcNow, null),
        };

        // Act
        var result = await ProcessReviewEventsBatch.Handle(
            reviewEvents, _bus, _validator, _logger, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<BadRequest<string>>();

        var badRequestResult = result.Result as BadRequest<string>;
        badRequestResult!.Value.Should().Contain("Validation failed");
        badRequestResult.Value.Should().Contain("LinesOfCodeReviewed is required for ReviewCompleted events");
    }

    [Fact]
    public async Task Handle_WithNegativeLinesOfCode_ReturnsBadRequest()
    {
        // Arrange
        var reviewEvents = new List<ReviewEvent>
        {
            new("ReviewCompleted", "review1", "author1", DateTime.UtcNow, -10)
        };

        // Act
        var result = await ProcessReviewEventsBatch.Handle(
            reviewEvents, _bus, _validator, _logger, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<BadRequest<string>>();

        var badRequestResult = result.Result as BadRequest<string>;
        badRequestResult!.Value.Should().Contain("Validation failed");
        badRequestResult.Value.Should().Contain("LinesOfCodeReviewed must be positive for ReviewCompleted events");
    }

    [Fact]
    public async Task Handle_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var reviewEvents = new List<ReviewEvent>
        {
            new("", "", "", DateTime.UtcNow, -5),
            new("ReviewCompleted", "review2", "author2", DateTime.UtcNow, null)
        };

        // Act
        var result = await ProcessReviewEventsBatch.Handle(
            reviewEvents, _bus, _validator, _logger, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<BadRequest<string>>();

        var badRequestResult = result.Result as BadRequest<string>;
        badRequestResult!.Value.Should().Contain("Validation failed");
        badRequestResult.Value.Should().Contain("Event 0:");
        badRequestResult.Value.Should().Contain("Event 1:");
        badRequestResult.Value.Should().Contain("Type is required and cannot be null or empty");
        badRequestResult.Value.Should().Contain("ReviewId is required and cannot be null or empty");
        badRequestResult.Value.Should().Contain("AuthorId is required and cannot be null or empty");
    }
}