namespace ReviewMetricsProcessor.Data.Entities;

public class Review
{
    public string Id { get; set; } = string.Empty;
    public DateTime StartedTimestamp { get; set; }
    public DateTime? CompletedTimestamp { get; set; }
    public int? LinesOfCode { get; set; }

}
