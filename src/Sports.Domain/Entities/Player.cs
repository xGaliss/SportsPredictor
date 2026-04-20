namespace Sports.Domain.Entities;

public class Player
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public int? TeamId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Height { get; set; } = string.Empty;
    public string Weight { get; set; } = string.Empty;
    public string JerseyNumber { get; set; } = string.Empty;
    public string College { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
