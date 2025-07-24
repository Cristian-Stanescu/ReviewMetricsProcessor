using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReviewMetricsProcessor.Consumers;
using ReviewMetricsProcessor.Data;
using ReviewMetricsProcessor.Data.Entities;
using ReviewMetricsProcessor.Messages;

namespace ReviewMetricsProcessor.Tests.Consumers;

public class ReviewCompletedConsumerTests : IDisposable
{
    private readonly ReviewMetricsDbContext _dbContext;
    private readonly ILogger<ReviewCompletedConsumer> _logger;
    private readonly ReviewCompletedConsumer _consumer;
    private readonly ConsumeContext<ReviewCompleted> _context;

    public ReviewCompletedConsumerTests()
    {
        var options = new DbContextOptionsBuilder<ReviewMetricsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ReviewMetricsDbContext(options);
        _logger = Substitute.For<ILogger<ReviewCompletedConsumer>>();
        _consumer = new ReviewCompletedConsumer(_dbContext, _logger);
        _context = Substitute.For<ConsumeContext<ReviewCompleted>>();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Consume_WithValidReview_CompletesReviewAndUpdatesMetrics()
    {
        // Arrange
        var completionTime = DateTime.UtcNow;
        var startTime = completionTime.AddHours(-2);
        var linesOfCode = 150;

        var existingAuthor = new Author
        {
            Id = "author1",
            Reviews =
            [
                new()
                {
                    Id = "review1",
                    StartedTimestamp = startTime,
                    CompletedTimestamp = null,
                    LinesOfCode = null
                }
            ]
        };
        _dbContext.Authors.Add(existingAuthor);
        await _dbContext.SaveChangesAsync();

        var message = new ReviewCompleted("review1", "author1", completionTime, linesOfCode);
        _context.Message.Returns(message);
        _context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(_context);

        // Assert
        var author = await _dbContext.Authors
            .Include(a => a.Reviews)
            .FirstOrDefaultAsync(a => a.Id == "author1");

        author.Should().NotBeNull();
        author!.Reviews.Should().HaveCount(1);

        var review = author.Reviews.First();
        review.Id.Should().Be("review1");
        review.StartedTimestamp.Should().Be(startTime);
        review.CompletedTimestamp.Should().Be(completionTime);
        review.LinesOfCode.Should().Be(linesOfCode);

        // Verify metrics were updated
        author.TotalLinesOfCodeReviewed.Should().Be(linesOfCode);
        author.LinesOfCodeReviewedPerHour.Should().Be(75);
        author.AverageReviewDuration.Should().Be(2 * 3600);
    }

    [Fact]
    public async Task Consume_WithNonExistentAuthor_ThrowsInvalidOperationException()
    {
        // Arrange
        var message = new ReviewCompleted("review1", "nonexistent-author", DateTime.UtcNow, 100);
        _context.Message.Returns(message);
        _context.CancellationToken.Returns(CancellationToken.None);

        // Act & Assert
        var act = async () => await _consumer.Consume(_context);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Author with ID nonexistent-author not found.");
    }

    [Fact]
    public async Task Consume_WithNonExistentReview_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingAuthor = new Author
        {
            Id = "author1",
            Reviews =
            [
                new() { Id = "different-review", StartedTimestamp = DateTime.UtcNow.AddHours(-1) }
            ]
        };
        _dbContext.Authors.Add(existingAuthor);
        await _dbContext.SaveChangesAsync();

        var message = new ReviewCompleted("nonexistent-review", "author1", DateTime.UtcNow, 100);
        _context.Message.Returns(message);
        _context.CancellationToken.Returns(CancellationToken.None);

        // Act & Assert
        var act = async () => await _consumer.Consume(_context);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Review with ID nonexistent-review and Author ID author1 not found.");
    }

    [Fact]
    public async Task Consume_WithAlreadyCompletedReview_ThrowsInvalidOperationException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-2);
        var firstCompletionTime = DateTime.UtcNow.AddHours(-1);

        var existingAuthor = new Author
        {
            Id = "author1",
            Reviews =
            [
                new()
                {
                    Id = "review1",
                    StartedTimestamp = startTime,
                    CompletedTimestamp = firstCompletionTime,
                    LinesOfCode = 50
                }
            ]
        };
        _dbContext.Authors.Add(existingAuthor);
        await _dbContext.SaveChangesAsync();

        var message = new ReviewCompleted("review1", "author1", DateTime.UtcNow, 100);
        _context.Message.Returns(message);
        _context.CancellationToken.Returns(CancellationToken.None);

        // Act & Assert
        var act = async () => await _consumer.Consume(_context);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Review with ID review1 and Author ID author1 has already been completed.");
    }

    [Fact]
    public async Task Consume_WithMultipleReviewsForSameAuthor_UpdatesMetricsCorrectly()
    {
        // Arrange
        var baseTime = DateTime.UtcNow.AddHours(-3);
        var existingAuthor = new Author
        {
            Id = "author1",
            Reviews =
            [
                new()
                {
                    Id = "review1",
                    StartedTimestamp = baseTime,
                    CompletedTimestamp = baseTime.AddHours(1),
                    LinesOfCode = 100
                },
                new()
                {
                    Id = "review2",
                    StartedTimestamp = baseTime.AddHours(1),
                    CompletedTimestamp = null,
                    LinesOfCode = null
                }
            ]
        };
        _dbContext.Authors.Add(existingAuthor);
        await _dbContext.SaveChangesAsync();

        var message = new ReviewCompleted("review2", "author1", baseTime.AddHours(2), 200);
        _context.Message.Returns(message);
        _context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(_context);

        // Assert
        var author = await _dbContext.Authors
            .Include(a => a.Reviews)
            .FirstOrDefaultAsync(a => a.Id == "author1");

        author.Should().NotBeNull();
        author!.Reviews.Should().HaveCount(2);
        author.TotalLinesOfCodeReviewed.Should().Be(100 + 200);
        author.CompletedReviews.Should().Be(2);
        author.LinesOfCodeReviewedPerHour.Should().Be(150);
        author.AverageReviewDuration.Should().Be(3600);
    }
}