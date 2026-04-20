using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sports.Api.Data;
using Sports.Api.Models;
using Sports.Domain.Entities;
using Sports.Infrastructure.External;

namespace Sports.Api.Services;

public class SeedService
{
    private readonly AppDbContext _dbContext;
    private readonly IBasketballDataProvider _provider;
    private readonly ILogger<SeedService> _logger;
    private readonly BalldontlieOptions _options;

    public SeedService(
        AppDbContext dbContext,
        IBasketballDataProvider provider,
        IOptions<BalldontlieOptions> options,
        ILogger<SeedService> logger)
    {
        _dbContext = dbContext;
        _provider = provider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<int> SeedTeamsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<BalldontlieTeamDto> externalTeams;

        try
        {
            externalTeams = await _provider.GetTeamsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudieron traer equipos desde el proveedor.");
            if (!_options.UseMockFallbackData) throw;
            externalTeams = GetMockTeams();
        }

        var created = 0;

        foreach (var externalTeam in externalTeams)
        {
            var team = await _dbContext.Teams.FirstOrDefaultAsync(x => x.ExternalId == externalTeam.Id.ToString(), cancellationToken);
            if (team is null)
            {
                team = new Team
                {
                    ExternalId = externalTeam.Id.ToString(),
                    Name = string.IsNullOrWhiteSpace(externalTeam.FullName) ? externalTeam.Name : externalTeam.FullName,
                    City = externalTeam.City,
                    Abbreviation = externalTeam.Abbreviation,
                    Conference = externalTeam.Conference,
                    Division = externalTeam.Division
                };
                _dbContext.Teams.Add(team);
                created++;
            }
            else
            {
                team.Name = string.IsNullOrWhiteSpace(externalTeam.FullName) ? externalTeam.Name : externalTeam.FullName;
                team.City = externalTeam.City;
                team.Abbreviation = externalTeam.Abbreviation;
                team.Conference = externalTeam.Conference;
                team.Division = externalTeam.Division;
                team.UpdatedUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<int> SeedGamesByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<BalldontlieGameDto> externalGames;

        try
        {
            externalGames = await _provider.GetGamesByDateAsync(date, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudieron traer partidos del proveedor para {Date}", date);
            if (!_options.UseMockFallbackData) throw;
            externalGames = await BuildMockGamesAsync(date, cancellationToken);
        }

        var teamLookup = await _dbContext.Teams.ToDictionaryAsync(x => x.ExternalId, x => x.Id, cancellationToken);
        var created = 0;

        foreach (var externalGame in externalGames)
        {
            if (!teamLookup.TryGetValue(externalGame.HomeTeam.Id.ToString(), out var homeTeamId))
            {
                continue;
            }

            if (!teamLookup.TryGetValue(externalGame.VisitorTeam.Id.ToString(), out var awayTeamId))
            {
                continue;
            }

            var game = await _dbContext.Games.FirstOrDefaultAsync(x => x.ExternalId == externalGame.Id.ToString(), cancellationToken);
            if (game is null)
            {
                game = new Game
                {
                    ExternalId = externalGame.Id.ToString(),
                    GameDateUtc = externalGame.GetScheduledDateTimeUtc(),
                    HomeTeamId = homeTeamId,
                    AwayTeamId = awayTeamId,
                    HomeScore = externalGame.HomeTeamScore,
                    AwayScore = externalGame.VisitorTeamScore,
                    Status = externalGame.Status,
                    Season = externalGame.Season,
                    IsCompleted = IsCompletedStatus(externalGame.Status)
                };
                _dbContext.Games.Add(game);
                created++;
            }
            else
            {
                game.GameDateUtc = externalGame.GetScheduledDateTimeUtc();
                game.HomeScore = externalGame.HomeTeamScore;
                game.AwayScore = externalGame.VisitorTeamScore;
                game.Status = externalGame.Status;
                game.Season = externalGame.Season;
                game.IsCompleted = IsCompletedStatus(externalGame.Status);
                game.UpdatedUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await EnsureBasicTeamStatsForCompletedGamesAsync(date, cancellationToken);
        return created;
    }

    private async Task EnsureBasicTeamStatsForCompletedGamesAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);

        var completedGames = await _dbContext.Games
            .Where(x => x.GameDateUtc >= start && x.GameDateUtc < end && x.IsCompleted)
            .ToListAsync(cancellationToken);

        foreach (var game in completedGames)
        {
            var existingStats = await _dbContext.TeamGameStats.CountAsync(x => x.GameId == game.Id, cancellationToken);
            if (existingStats > 0) continue;

            _dbContext.TeamGameStats.Add(new TeamGameStat
            {
                GameId = game.Id,
                TeamId = game.HomeTeamId,
                Points = game.HomeScore ?? 0,
                Rebounds = 42,
                Assists = 24,
                Turnovers = 13,
                FieldGoalPercentage = 0.47m,
                ThreePointPercentage = 0.36m,
                FreeThrowPercentage = 0.79m
            });

            _dbContext.TeamGameStats.Add(new TeamGameStat
            {
                GameId = game.Id,
                TeamId = game.AwayTeamId,
                Points = game.AwayScore ?? 0,
                Rebounds = 40,
                Assists = 22,
                Turnovers = 14,
                FieldGoalPercentage = 0.45m,
                ThreePointPercentage = 0.34m,
                FreeThrowPercentage = 0.77m
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsCompletedStatus(string status)
        => status.Contains("Final", StringComparison.OrdinalIgnoreCase) ||
           status.Contains("Finished", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyCollection<BalldontlieTeamDto> GetMockTeams()
        => new List<BalldontlieTeamDto>
        {
            new() { Id = 14, Abbreviation = "LAL", City = "Los Angeles", Conference = "West", Division = "Pacific", FullName = "Los Angeles Lakers", Name = "Lakers" },
            new() { Id = 10, Abbreviation = "GSW", City = "Golden State", Conference = "West", Division = "Pacific", FullName = "Golden State Warriors", Name = "Warriors" },
            new() { Id = 2, Abbreviation = "BOS", City = "Boston", Conference = "East", Division = "Atlantic", FullName = "Boston Celtics", Name = "Celtics" },
            new() { Id = 21, Abbreviation = "NYK", City = "New York", Conference = "East", Division = "Atlantic", FullName = "New York Knicks", Name = "Knicks" }
        };

    private async Task<IReadOnlyCollection<BalldontlieGameDto>> BuildMockGamesAsync(DateOnly date, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Teams.AnyAsync(cancellationToken))
        {
            await SeedTeamsAsync(cancellationToken);
        }
        return new List<BalldontlieGameDto>
{
    new()
    {
        Id = int.Parse(date.ToString("yyyyMMdd")) + 1,
        Datetime = date.ToDateTime(new TimeOnly(23, 30), DateTimeKind.Utc),
        Season = date.Month >= 10 ? date.Year : date.Year - 1,
        Status = "Scheduled",
        HomeTeam = new BalldontlieTeamDto { Id = 14, FullName = "Los Angeles Lakers", Name = "Lakers", Abbreviation = "LAL", City = "Los Angeles", Conference = "West", Division = "Pacific" },
        VisitorTeam = new BalldontlieTeamDto { Id = 10, FullName = "Golden State Warriors", Name = "Warriors", Abbreviation = "GSW", City = "Golden State", Conference = "West", Division = "Pacific" }
    },
    new()
    {
        Id = int.Parse(date.ToString("yyyyMMdd")) + 2,
        Datetime = date.ToDateTime(new TimeOnly(0, 30), DateTimeKind.Utc).AddDays(1),
        Season = date.Month >= 10 ? date.Year : date.Year - 1,
        Status = "Scheduled",
        HomeTeam = new BalldontlieTeamDto { Id = 2, FullName = "Boston Celtics", Name = "Celtics", Abbreviation = "BOS", City = "Boston", Conference = "East", Division = "Atlantic" },
        VisitorTeam = new BalldontlieTeamDto { Id = 21, FullName = "New York Knicks", Name = "Knicks", Abbreviation = "NYK", City = "New York", Conference = "East", Division = "Atlantic" }
    }
};
    }
}
