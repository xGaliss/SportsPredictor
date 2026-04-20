namespace Sports.Domain.Entities;

public class Game
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public DateTime GameDateUtc { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Season { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
