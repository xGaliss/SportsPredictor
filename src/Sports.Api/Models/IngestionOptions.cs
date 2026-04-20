namespace Sports.Api.Models;

public class IngestionOptions
{
    public bool Enabled { get; set; }
    public bool RunOnStartup { get; set; }
    public int IntervalMinutes { get; set; } = 180;
}
