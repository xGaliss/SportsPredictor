namespace Sports.Domain.Entities;

public class PlayerGameStat
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public int Points { get; set; }
    public int Rebounds { get; set; }
    public int Assists { get; set; }
    public int Steals { get; set; }
    public int Blocks { get; set; }
    public int Turnovers { get; set; }
    public decimal Minutes { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
