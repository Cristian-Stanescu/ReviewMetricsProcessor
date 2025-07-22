using ReviewMetricsProcessor.Data.Entities;

namespace ReviewMetricsProcessor.Tests.Data;

public class AuthorTests
{
    private const double Tolerance = 0.01; // Tolerance for double comparisons

    [Fact]
    public void UpdateMetrics_WithNoReviews_SetsAllMetricsToZeroOrNull()
    {
        // Arrange
        var author = new Author
        {
            Id = "author1",
            Reviews = []
        };

        // Act
        author.UpdateMetrics();

        // Assert
        Assert.Equal(0, author.TotalLinesOfCodeReviewed);
        Assert.Equal(0, author.LinesOfCodeReviewedPerHour);
        Assert.Null(author.AverageReviewDuration);
    }

    [Fact]
    public void UpdateMetrics_WithOnlyIncompleteReviews_SetsAllMetricsToZeroOrNull()
    {
        // Arrange
        var author = new Author
        {
            Id = "author1",
            Reviews = 
            [
                new Review
                {
                    Id = "review1",
                    StartedTimestamp = DateTime.Now.AddHours(-2),
                    CompletedTimestamp = null,
                    LinesOfCode = 100
                },
                new Review
                {
                    Id = "review2", 
                    StartedTimestamp = DateTime.Now.AddHours(-1),
                    CompletedTimestamp = null,
                    LinesOfCode = 200
                }
            ]
        };

        // Act
        author.UpdateMetrics();

        // Assert
        Assert.Equal(0, author.TotalLinesOfCodeReviewed);
        Assert.Equal(0, author.LinesOfCodeReviewedPerHour);
        Assert.Null(author.AverageReviewDuration);
    }

    [Fact]
    public void UpdateMetrics_WithSingleCompletedReview_CalculatesMetricsCorrectly()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var endTime = new DateTime(2024, 1, 1, 11, 0, 0); // Exactly 1 hour
        var author = new Author
        {
            Id = "author1",
            Reviews = 
            [
                new Review
                {
                    Id = "review1",
                    StartedTimestamp = startTime,
                    CompletedTimestamp = endTime,
                    LinesOfCode = 360 // 360 lines in 1 hour = 360 lines/hour
                }
            ]
        };

        // Act
        author.UpdateMetrics();

        // Assert
        Assert.Equal(360, author.TotalLinesOfCodeReviewed);
        Assert.Equal(360, author.LinesOfCodeReviewedPerHour, Tolerance); // 360 lines in 1 hour
        Assert.Equal(3600, author.AverageReviewDuration); // 1 hour = 3600 seconds
    }

    [Fact]
    public void UpdateMetrics_WithMultipleCompletedReviews_CalculatesMetricsCorrectly()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var author = new Author
        {
            Id = "author1",
            Reviews = 
            [
                new Review
                {
                    Id = "review1",
                    StartedTimestamp = baseTime,
                    CompletedTimestamp = baseTime.AddHours(1), // 1 hour duration
                    LinesOfCode = 100
                },
                new Review
                {
                    Id = "review2",
                    StartedTimestamp = baseTime.AddHours(1),
                    CompletedTimestamp = baseTime.AddHours(3), // 2 hour duration
                    LinesOfCode = 200
                },
                new Review
                {
                    Id = "review3",
                    StartedTimestamp = baseTime.AddHours(-1),
                    CompletedTimestamp = null, // Incomplete - should be ignored
                    LinesOfCode = 500
                }
            ]
        };

        // Act
        author.UpdateMetrics();

        // Assert
        Assert.Equal(300, author.TotalLinesOfCodeReviewed); // 100 + 200
        // Total duration: 3600 + 7200 = 10800 seconds (3 hours)
        // Lines per hour: 300 / (10800/3600) = 300 / 3 = 100
        Assert.Equal(100, author.LinesOfCodeReviewedPerHour, Tolerance);
        // Average duration: 10800 / 2 = 5400 seconds (1.5 hours)
        Assert.Equal(5400, author.AverageReviewDuration);
    }

    [Fact]
    public void UpdateMetrics_WithNullLinesOfCode_TreatsAsZero()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var endTime = new DateTime(2024, 1, 1, 11, 0, 0); // Exactly 1 hour
        var author = new Author
        {
            Id = "author1",
            Reviews = 
            [
                new Review
                {
                    Id = "review1",
                    StartedTimestamp = startTime,
                    CompletedTimestamp = endTime,
                    LinesOfCode = null
                },
                new Review
                {
                    Id = "review2",
                    StartedTimestamp = startTime,
                    CompletedTimestamp = endTime,
                    LinesOfCode = 100
                }
            ]
        };

        // Act
        author.UpdateMetrics();

        // Assert
        Assert.Equal(100, author.TotalLinesOfCodeReviewed); // null treated as 0, so 0 + 100
        Assert.Equal(50, author.LinesOfCodeReviewedPerHour, Tolerance); // 100 lines in 2 hours = 50 lines/hour
        Assert.Equal(3600, author.AverageReviewDuration); // 1 hour average
    }

    [Fact]
    public void UpdateMetrics_WithZeroDuration_SetsLinesPerHourToZero()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 1, 10, 0, 0);
        var author = new Author
        {
            Id = "author1",
            Reviews = 
            [
                new Review
                {
                    Id = "review1",
                    StartedTimestamp = timestamp,
                    CompletedTimestamp = timestamp, // Same time = 0 duration
                    LinesOfCode = 100
                }
            ]
        };

        // Act
        author.UpdateMetrics();

        // Assert
        Assert.Equal(100, author.TotalLinesOfCodeReviewed);
        Assert.Equal(0, author.LinesOfCodeReviewedPerHour); // Zero duration should result in 0 lines/hour
        Assert.Equal(0, author.AverageReviewDuration);
    }

    [Fact]
    public void UpdateMetrics_WithMixedCompletedAndIncompleteReviews_OnlyProcessesCompleted()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var author = new Author
        {
            Id = "author1",
            Reviews = 
            [
                new Review
                {
                    Id = "review1",
                    StartedTimestamp = baseTime,
                    CompletedTimestamp = baseTime.AddHours(1),
                    LinesOfCode = 100
                },
                new Review
                {
                    Id = "review2",
                    StartedTimestamp = baseTime.AddHours(1),
                    CompletedTimestamp = null, // Incomplete
                    LinesOfCode = 999 // Should be ignored
                },
                new Review
                {
                    Id = "review3",
                    StartedTimestamp = baseTime.AddMinutes(30),
                    CompletedTimestamp = baseTime.AddHours(1.5),
                    LinesOfCode = 200
                }
            ]
        };

        // Act
        author.UpdateMetrics();

        // Assert
        Assert.Equal(300, author.TotalLinesOfCodeReviewed); // Only completed reviews: 100 + 200
        // Review1: 1 hour, Review3: 1 hour, Total: 7200 seconds
        // Lines per hour: 300 / (7200/3600) = 300 / 2 = 150
        Assert.Equal(150, author.LinesOfCodeReviewedPerHour, Tolerance);
        Assert.Equal(3600, author.AverageReviewDuration); // (3600 + 3600) / 2 = 3600
    }

    [Fact]
    public void UpdateMetrics_AfterPreviousCalculation_RecalculatesCorrectly()
    {
        // Arrange
        var baseTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var author = new Author
        {
            Id = "author1",
            Reviews = 
            [
                new Review
                {
                    Id = "review1",
                    StartedTimestamp = baseTime,
                    CompletedTimestamp = baseTime.AddHours(1),
                    LinesOfCode = 100
                }
            ]
        };

        // Act - First calculation
        author.UpdateMetrics();
        var firstTotalLines = author.TotalLinesOfCodeReviewed;
        var firstLinesPerHour = author.LinesOfCodeReviewedPerHour;

        // Add another completed review
        author.Reviews.Add(new Review
        {
            Id = "review2",
            StartedTimestamp = baseTime.AddHours(1),
            CompletedTimestamp = baseTime.AddHours(2),
            LinesOfCode = 200
        });

        // Act - Second calculation
        author.UpdateMetrics();

        // Assert
        Assert.NotEqual(firstTotalLines, author.TotalLinesOfCodeReviewed);
        Assert.NotEqual(firstLinesPerHour, author.LinesOfCodeReviewedPerHour);
        Assert.Equal(300, author.TotalLinesOfCodeReviewed); // 100 + 200
        Assert.Equal(150, author.LinesOfCodeReviewedPerHour, Tolerance); // 300 lines in 2 hours
        Assert.Equal(3600, author.AverageReviewDuration); // Average of 1 hour each
    }
}