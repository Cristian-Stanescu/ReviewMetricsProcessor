using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReviewMetricsProcessor.Consumers;
using ReviewMetricsProcessor.Data;
using ReviewMetricsProcessor.Data.Entities;
using ReviewMetricsProcessor.Messages;

namespace ReviewMetricsProcessor.Tests.Consumers;

public class ReviewStartedConsumerTests : IDisposable
{
    private readonly ReviewMetricsDbContext _dbContext;
    private readonly ILogger<ReviewStartedConsumer> _logger;
    private readonly ReviewStartedConsumer _consumer;
    private readonly ConsumeContext<ReviewStartedMessage> _context;

    public ReviewStartedConsumerTests()
    {
        var options = new DbContextOptionsBuilder<ReviewMetricsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ReviewMetricsDbContext(options);
        _logger = Substitute.For<ILogger<ReviewStartedConsumer>>();
        _consumer = new ReviewStartedConsumer(_dbContext, _logger);
        _context = Substitute.For<ConsumeContext<ReviewStartedMessage>>();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Consume_WithNewAuthor_CreatesAuthorAndReview()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var message = new ReviewStartedMessage("review1", "author1", timestamp);
        _context.Message.Returns(message);
        _context.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _consumer.Consume(_context);

        // Assert
        var author = await _dbContext.Authors
            .Include(a => a.Reviews)
            .FirstOrDefaultAsync(a => a.Id == "author1");

        author.Should().NotBeNull();
        author!.Id.Should().Be("author1");
        author.Reviews.Should().HaveCount(1);

        var review = author.Reviews.First();
        review.Id.Should().Be("review1");
        review.StartedTimestamp.Should().Be(timestamp);
        review.CompletedTimestamp.Should().BeNull();
        review.LinesOfCode.Should().BeNull();
    }

    [Fact]
    public async Task Consume_WithExistingAuthor_AddsNewReview()
    {
        // Arrange
        var existingTimestamp = DateTime.UtcNow.AddDays(-1);
        var existingAuthor = new Author
        {
            Id = "author1",
            Reviews =
            [
                new() { Id = "existing-review", StartedTimestamp = existingTimestamp }
            ]
        };
        _dbContext.Authors.Add(existingAuthor);
        await _dbContext.SaveChangesAsync();

        var newTimestamp = DateTime.UtcNow;
        var message = new ReviewStartedMessage("review2", "author1", newTimestamp);
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

        var newReview = author.Reviews.FirstOrDefault(r => r.Id == "review2");
        newReview.Should().NotBeNull();
        newReview!.StartedTimestamp.Should().Be(newTimestamp);

        var existingReview = author.Reviews.FirstOrDefault(r => r.Id == "existing-review");
        existingReview.Should().NotBeNull();
        existingReview!.StartedTimestamp.Should().Be(existingTimestamp);
    }

    [Fact]
    public async Task Consume_WithExistingReview_UpdatesStartedTimestamp()
    {
        // Arrange
        var originalTimestamp = DateTime.UtcNow.AddHours(-1);
        var existingAuthor = new Author
        {
            Id = "author1",
            Reviews =
            [
                new() { Id = "review1", StartedTimestamp = originalTimestamp }
            ]
        };
        _dbContext.Authors.Add(existingAuthor);
        await _dbContext.SaveChangesAsync();

        var newTimestamp = DateTime.UtcNow;
        var message = new ReviewStartedMessage("review1", "author1", newTimestamp);
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
        review.StartedTimestamp.Should().Be(newTimestamp);
        review.StartedTimestamp.Should().NotBe(originalTimestamp);
    }

    [Fact]
    public async Task Consume_WithMultipleConsecutiveReviewsForSameAuthor_HandlesCorrectly()
    {
        // Arrange
        var timestamp1 = DateTime.UtcNow.AddMinutes(-10);
        var timestamp2 = DateTime.UtcNow.AddMinutes(-5);
        var timestamp3 = DateTime.UtcNow;

        var message1 = new ReviewStartedMessage("review1", "author1", timestamp1);
        var message2 = new ReviewStartedMessage("review2", "author1", timestamp2);
        var message3 = new ReviewStartedMessage("review3", "author1", timestamp3);

        _context.CancellationToken.Returns(CancellationToken.None);

        // Act
        _context.Message.Returns(message1);
        await _consumer.Consume(_context);

        _context.Message.Returns(message2);
        await _consumer.Consume(_context);

        _context.Message.Returns(message3);
        await _consumer.Consume(_context);

        // Assert
        var author = await _dbContext.Authors.Include(a => a.Reviews).FirstOrDefaultAsync(a => a.Id == "author1");
        author.Should().NotBeNull();
        author!.Reviews.Should().HaveCount(3);

        var reviews = author.Reviews.OrderBy(r => r.StartedTimestamp).ToList();
        reviews[0].Id.Should().Be("review1");
        reviews[0].StartedTimestamp.Should().Be(timestamp1);
        reviews[1].Id.Should().Be("review2");
        reviews[1].StartedTimestamp.Should().Be(timestamp2);
        reviews[2].Id.Should().Be("review3");
        reviews[2].StartedTimestamp.Should().Be(timestamp3);
    }

    [Fact]
    public async Task Consume_WithDifferentAuthors_CreatesMultipleAuthors()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var message1 = new ReviewStartedMessage("review1", "author1", timestamp);
        var message2 = new ReviewStartedMessage("review2", "author2", timestamp.AddMinutes(5));

        _context.CancellationToken.Returns(CancellationToken.None);

        // Act
        _context.Message.Returns(message1);
        await _consumer.Consume(_context);

        _context.Message.Returns(message2);
        await _consumer.Consume(_context);

        // Assert
        var authors = await _dbContext.Authors.Include(a => a.Reviews).ToListAsync();
        authors.Should().HaveCount(2);

        var author1 = authors.FirstOrDefault(a => a.Id == "author1");
        var author2 = authors.FirstOrDefault(a => a.Id == "author2");

        author1.Should().NotBeNull();
        author2.Should().NotBeNull();

        author1!.Reviews.Should().HaveCount(1);
        author2!.Reviews.Should().HaveCount(1);

        author1.Reviews.First().Id.Should().Be("review1");
        author2.Reviews.First().Id.Should().Be("review2");
    }
}