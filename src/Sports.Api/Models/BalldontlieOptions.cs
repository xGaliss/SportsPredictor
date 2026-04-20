namespace Sports.Api.Models;

public class BalldontlieOptions
{
    public string BaseUrl { get; set; } = "https://api.balldontlie.io";
    public string ApiKey { get; set; } = string.Empty;
    public bool UseMockFallbackData { get; set; } = true;
}
