using System.Text.Json.Serialization;

namespace Sports.Infrastructure.External;

public class BalldontlieListResponse<T>
{
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();

    [JsonPropertyName("meta")]
    public BalldontlieMetaDto? Meta { get; set; }
}

public class BalldontlieMetaDto
{
    [JsonPropertyName("next_cursor")]
    public int? NextCursor { get; set; }

    [JsonPropertyName("per_page")]
    public int? PerPage { get; set; }
}

public class BalldontlieTeamDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("conference")]
    public string Conference { get; set; } = string.Empty;

    [JsonPropertyName("division")]
    public string Division { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class BalldontlieGameDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("datetime")]
    public DateTime? Datetime { get; set; }

    [JsonPropertyName("season")]
    public int Season { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("home_team")]
    public BalldontlieTeamDto HomeTeam { get; set; } = new();

    [JsonPropertyName("visitor_team")]
    public BalldontlieTeamDto VisitorTeam { get; set; } = new();

    [JsonPropertyName("home_team_score")]
    public int? HomeTeamScore { get; set; }

    [JsonPropertyName("visitor_team_score")]
    public int? VisitorTeamScore { get; set; }

    public DateTime GetScheduledDateTimeUtc()
    {
        if (Datetime.HasValue)
        {
            return Datetime.Value.Kind == DateTimeKind.Utc
                ? Datetime.Value
                : Datetime.Value.ToUniversalTime();
        }

        if (!string.IsNullOrWhiteSpace(Date) &&
            DateTime.TryParse(Date, out var parsed))
        {
            return parsed.Kind == DateTimeKind.Utc
                ? parsed
                : parsed.ToUniversalTime();
        }

        return DateTime.UtcNow;
    }
}
