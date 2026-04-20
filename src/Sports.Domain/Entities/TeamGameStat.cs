namespace Sports.Domain.Entities;

public class TeamGameStat
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int TeamId { get; set; }
    public int Points { get; set; }
    public int Rebounds { get; set; }
    public int Assists { get; set; }
    public int Turnovers { get; set; }
    public decimal FieldGoalPercentage { get; set; }
    public decimal ThreePointPercentage { get; set; }
    public decimal FreeThrowPercentage { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
