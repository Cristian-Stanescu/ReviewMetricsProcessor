using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReviewMetricsProcessor.Consumers;
using ReviewMetricsProcessor.Data;
using ReviewMetricsProcessor.Data.Entities;
using ReviewMetricsProcessor.Messages;

namespace ReviewMetricsProcessor.Tests;

public class MassTransitIntegrationTests : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _databaseName;

    public MassTransitIntegrationTests()
    {
        _databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        // Add DbContext with a specific database name that will be shared across all instances
        services.AddDbContext<ReviewMetricsDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: _databaseName));

        // Add logging
        services.AddLogging();

        // Add MassTransit with test harness
        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<ReviewStartedConsumer>();
            x.AddConsumer<ReviewCompletedConsumer>();
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task ReviewStartedConsumer_WithNewAuthor_CreatesAuthorAndReview()
    {
        // Arrange
        var harness = _serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var message = new ReviewStarted("review1", "author1", DateTime.UtcNow);

            // Act
            await harness.Bus.Publish(message);

            // Wait for message processing
            Assert.True(await harness.Published.Any<ReviewStarted>());
            Assert.True(await harness.Consumed.Any<ReviewStarted>());

            // Give a moment for database operations to complete
            await Task.Delay(200);

            // Assert - Use a new scope to get a fresh DbContext instance
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ReviewMetricsDbContext>();

            var author = await dbContext.Authors
                .Include(a => a.Reviews)
                .FirstOrDefaultAsync(a => a.Id == "author1");

            Assert.NotNull(author);
            Assert.Equal("author1", author.Id);
            Assert.Single(author.Reviews);

            var review = author.Reviews.First();
            Assert.Equal("review1", review.Id);
            Assert.Equal(message.Timestamp, review.StartedTimestamp);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task ReviewCompletedConsumer_WithValidReview_CompletesReviewAndUpdatesMetrics()
    {
        // Arrange
        var harness = _serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var startTime = DateTime.UtcNow.AddHours(-1);
            var completionTime = DateTime.UtcNow;

            // Setup test data using a scoped DbContext
            using (var setupScope = _serviceProvider.CreateScope())
            {
                var setupDbContext = setupScope.ServiceProvider.GetRequiredService<ReviewMetricsDbContext>();
                var existingAuthor = new Author
                {
                    Id = "author1",
                    Reviews =
                    [
                        new Review
                        {
                            Id = "review1",
                            StartedTimestamp = startTime,
                            CompletedTimestamp = null,
                            LinesOfCode = null
                        }
                    ]
                };
                setupDbContext.Authors.Add(existingAuthor);
                await setupDbContext.SaveChangesAsync();
            }

            var message = new ReviewCompleted("review1", "author1", completionTime, 150);

            // Act
            await harness.Bus.Publish(message);

            // Wait for message processing
            Assert.True(await harness.Published.Any<ReviewCompleted>());
            Assert.True(await harness.Consumed.Any<ReviewCompleted>());

            // Give a moment for database operations to complete
            await Task.Delay(200);

            // Assert - Use a new scope to get a fresh DbContext instance
            using var assertScope = _serviceProvider.CreateScope();
            var assertDbContext = assertScope.ServiceProvider.GetRequiredService<ReviewMetricsDbContext>();

            var author = await assertDbContext.Authors
                .Include(a => a.Reviews)
                .FirstOrDefaultAsync(a => a.Id == "author1");

            Assert.NotNull(author);
            var review = author.Reviews.First();
            Assert.Equal(completionTime, review.CompletedTimestamp);
            Assert.Equal(150, review.LinesOfCode);

            // Verify metrics were updated
            Assert.Equal(150, author.TotalLinesOfCodeReviewed);
            Assert.True(author.LinesOfCodeReviewedPerHour > 0);
            Assert.True(author.AverageReviewDuration > 0);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task ReviewCompletedConsumer_WithNonExistentAuthor_HandlesError()
    {
        // Arrange
        var harness = _serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var message = new ReviewCompleted("review1", "nonexistent", DateTime.UtcNow, 100);

            // Act & Assert
            await harness.Bus.Publish(message);

            Assert.True(await harness.Published.Any<ReviewCompleted>());

            // The consumer should handle the error (we expect it to fail)
            var consumerHarness = harness.GetConsumerHarness<ReviewCompletedConsumer>();
            Assert.True(await consumerHarness.Consumed.Any<ReviewCompleted>());

            // Since the consumer throws an exception, MassTransit will retry and eventually move to error queue
            // We can verify that the consumer processed the message (even if it failed)
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task FullWorkflow_StartThenComplete_WorksCorrectly()
    {
        // Arrange
        var harness = _serviceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var startTime = DateTime.UtcNow.AddHours(-1);
            var completionTime = DateTime.UtcNow;

            var startMessage = new ReviewStarted("review1", "author1", startTime);
            var completeMessage = new ReviewCompleted("review1", "author1", completionTime, 250);

            // Act
            await harness.Bus.Publish(startMessage);

            // Wait for the first message to be processed
            Assert.True(await harness.Published.Any<ReviewStarted>());
            Assert.True(await harness.Consumed.Any<ReviewStarted>());

            // Give time for database operations
            await Task.Delay(200);

            await harness.Bus.Publish(completeMessage);

            // Wait for the second message to be processed
            Assert.True(await harness.Published.Any<ReviewCompleted>());
            Assert.True(await harness.Consumed.Any<ReviewCompleted>());

            // Give time for database operations
            await Task.Delay(200);

            // Assert - Use a new scope to get a fresh DbContext instance
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ReviewMetricsDbContext>();

            var author = await dbContext.Authors
                .Include(a => a.Reviews)
                .FirstOrDefaultAsync(a => a.Id == "author1");

            Assert.NotNull(author);
            Assert.Single(author.Reviews);

            var review = author.Reviews.First();
            Assert.Equal("review1", review.Id);
            Assert.Equal(startTime, review.StartedTimestamp);
            Assert.Equal(completionTime, review.CompletedTimestamp);
            Assert.Equal(250, review.LinesOfCode);

            // Verify metrics
            Assert.Equal(250, author.TotalLinesOfCodeReviewed);
            Assert.True(author.LinesOfCodeReviewedPerHour > 0);
            Assert.True(author.AverageReviewDuration > 0);
        }
        finally
        {
            await harness.Stop();
        }
    }
}