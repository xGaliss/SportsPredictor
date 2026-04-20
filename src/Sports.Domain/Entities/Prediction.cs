namespace Sports.Domain.Entities;

public class Prediction
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public decimal HomeWinProbability { get; set; }
    public decimal AwayWinProbability { get; set; }
    public decimal HomeRating { get; set; }
    public decimal AwayRating { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime CalculatedUtc { get; set; } = DateTime.UtcNow;
}
