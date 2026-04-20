using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sports.Api.Background;
using Sports.Api.Data;
using Sports.Api.Models;
using Sports.Api.Services;
using Sports.Infrastructure.External;

var builder = WebApplication.CreateBuilder(args);

var balldontlieSection = builder.Configuration.GetSection("Balldontlie");
if (!balldontlieSection.Exists())
{
    balldontlieSection = builder.Configuration.GetSection("ExternalApis:Balldontlie");
}

builder.Services.Configure<BalldontlieOptions>(balldontlieSection);
builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection("Ingestion"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=sports-predictor.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<SeedService>();
builder.Services.AddScoped<PredictionService>();

builder.Services.AddHttpClient<IBasketballDataProvider, BalldontlieDataProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<BalldontlieOptions>>().Value;

    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Remove("Authorization");

    if (!string.IsNullOrWhiteSpace(options.ApiKey) &&
        !string.Equals(options.ApiKey, "PUT_YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase))
    {
        client.DefaultRequestHeaders.Add("Authorization", options.ApiKey);
    }
});

builder.Services.AddHostedService<IngestionBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/admin/config/balldontlie", (IOptions<BalldontlieOptions> options) =>
{
    var value = options.Value;
    return Results.Ok(new
    {
        value.BaseUrl,
        HasApiKey = !string.IsNullOrWhiteSpace(value.ApiKey) &&
                    !string.Equals(value.ApiKey, "PUT_YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase),
        value.UseMockFallbackData
    });
});

app.MapGet("/api/status", async (AppDbContext db, IOptions<BalldontlieOptions> options, CancellationToken ct) =>
{
    var window = GetSportsWindow();
    var utcRange = GetUtcRangeForWindow(window);

    var teamsCount = await db.Teams.CountAsync(ct);
    var gamesCount = await db.Games.CountAsync(
        x => x.GameDateUtc >= utcRange.StartUtc && x.GameDateUtc < utcRange.EndUtc, ct);

    var gameIds = await db.Games
        .Where(x => x.GameDateUtc >= utcRange.StartUtc && x.GameDateUtc < utcRange.EndUtc)
        .Select(x => x.Id)
        .ToListAsync(ct);

    var predictionsCount = await db.Predictions.CountAsync(x => gameIds.Contains(x.GameId), ct);
    var lastGameSyncUtc = await db.Games.MaxAsync(x => (DateTime?)x.UpdatedUtc, ct);
    var config = options.Value;

    return Results.Ok(new
    {
        localDate = window.Today.ToString("yyyy-MM-dd"),
        windowStart = window.Start.ToString("yyyy-MM-dd"),
        windowEnd = window.End.ToString("yyyy-MM-dd"),
        teamsCount,
        gamesWindowCount = gamesCount,
        predictionsWindowCount = predictionsCount,
        lastSyncUtc = lastGameSyncUtc,
        providerBaseUrl = config.BaseUrl,
        providerConfigured = !string.IsNullOrWhiteSpace(config.ApiKey) &&
                             !string.Equals(config.ApiKey, "PUT_YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase),
        useMockFallbackData = config.UseMockFallbackData
    });
});

app.MapGet("/api/games/today", async (AppDbContext db, CancellationToken ct) =>
{
    var sportsDate = GetSportsLocalDate();
    var utcRange = GetUtcRangeForLocalDate(sportsDate);

    var teams = await db.Teams.ToDictionaryAsync(x => x.Id, ct);
    var games = await db.Games
        .Where(x => x.GameDateUtc >= utcRange.StartUtc && x.GameDateUtc < utcRange.EndUtc)
        .OrderBy(x => x.GameDateUtc)
        .ToListAsync(ct);

    var result = games.Select(g => new
    {
        g.Id,
        g.ExternalId,
        g.GameDateUtc,
        SportsDateLocal = sportsDate.ToString("yyyy-MM-dd"),
        g.Status,
        g.Season,
        g.IsCompleted,
        HomeTeam = teams.TryGetValue(g.HomeTeamId, out var home) ? new { home.Id, home.Name, home.Abbreviation, home.City } : null,
        AwayTeam = teams.TryGetValue(g.AwayTeamId, out var away) ? new { away.Id, away.Name, away.Abbreviation, away.City } : null,
        g.HomeScore,
        g.AwayScore
    });

    return Results.Ok(result);
});

app.MapGet("/api/games/window", async (AppDbContext db, CancellationToken ct) =>
{
    var window = GetSportsWindow();
    var utcRange = GetUtcRangeForWindow(window);

    var teams = await db.Teams.ToDictionaryAsync(x => x.Id, ct);
    var games = await db.Games
        .Where(x => x.GameDateUtc >= utcRange.StartUtc && x.GameDateUtc < utcRange.EndUtc)
        .OrderBy(x => x.GameDateUtc)
        .ToListAsync(ct);

    var result = games.Select(g =>
    {
        var localDate = ConvertUtcToMadrid(g.GameDateUtc);
        return new
        {
            g.Id,
            g.ExternalId,
            g.GameDateUtc,
            SportsDateLocal = DateOnly.FromDateTime(localDate).ToString("yyyy-MM-dd"),
            LocalTime = localDate.ToString("HH:mm"),
            g.Status,
            g.Season,
            g.IsCompleted,
            HomeTeam = teams.TryGetValue(g.HomeTeamId, out var home) ? new { home.Id, home.Name, home.Abbreviation, home.City } : null,
            AwayTeam = teams.TryGetValue(g.AwayTeamId, out var away) ? new { away.Id, away.Name, away.Abbreviation, away.City } : null,
            g.HomeScore,
            g.AwayScore
        };
    });

    return Results.Ok(result);
});

app.MapGet("/api/predictions/today", async (AppDbContext db, CancellationToken ct) =>
{
    var sportsDate = GetSportsLocalDate();
    var utcRange = GetUtcRangeForLocalDate(sportsDate);

    var teams = await db.Teams.ToDictionaryAsync(x => x.Id, ct);
    var games = await db.Games
        .Where(x => x.GameDateUtc >= utcRange.StartUtc && x.GameDateUtc < utcRange.EndUtc)
        .ToDictionaryAsync(x => x.Id, ct);

    var gameIds = games.Keys.ToList();

    var predictions = await db.Predictions
        .Where(x => gameIds.Contains(x.GameId))
        .OrderByDescending(x => x.CalculatedUtc)
        .ToListAsync(ct);

    var result = predictions.Select(p =>
    {
        var game = games[p.GameId];
        teams.TryGetValue(game.HomeTeamId, out var home);
        teams.TryGetValue(game.AwayTeamId, out var away);

        return new
        {
            p.Id,
            p.GameId,
            GameDateUtc = game.GameDateUtc,
            SportsDateLocal = sportsDate.ToString("yyyy-MM-dd"),
            HomeTeam = home is null ? null : new { home.Id, home.Name, home.Abbreviation },
            AwayTeam = away is null ? null : new { away.Id, away.Name, away.Abbreviation },
            p.HomeRating,
            p.AwayRating,
            p.HomeWinProbability,
            p.AwayWinProbability,
            p.Summary,
            p.CalculatedUtc
        };
    });

    return Results.Ok(result);
});

app.MapGet("/api/predictions/window", async (AppDbContext db, CancellationToken ct) =>
{
    var window = GetSportsWindow();
    var utcRange = GetUtcRangeForWindow(window);

    var teams = await db.Teams.ToDictionaryAsync(x => x.Id, ct);
    var games = await db.Games
        .Where(x => x.GameDateUtc >= utcRange.StartUtc && x.GameDateUtc < utcRange.EndUtc)
        .ToDictionaryAsync(x => x.Id, ct);

    var gameIds = games.Keys.ToList();

    var predictions = await db.Predictions
        .Where(x => gameIds.Contains(x.GameId))
        .OrderByDescending(x => x.CalculatedUtc)
        .ToListAsync(ct);

    var result = predictions.Select(p =>
    {
        var game = games[p.GameId];
        teams.TryGetValue(game.HomeTeamId, out var home);
        teams.TryGetValue(game.AwayTeamId, out var away);
        var localDate = ConvertUtcToMadrid(game.GameDateUtc);

        return new
        {
            p.Id,
            p.GameId,
            GameDateUtc = game.GameDateUtc,
            SportsDateLocal = DateOnly.FromDateTime(localDate).ToString("yyyy-MM-dd"),
            LocalTime = localDate.ToString("HH:mm"),
            HomeTeam = home is null ? null : new { home.Id, home.Name, home.Abbreviation },
            AwayTeam = away is null ? null : new { away.Id, away.Name, away.Abbreviation },
            p.HomeRating,
            p.AwayRating,
            p.HomeWinProbability,
            p.AwayWinProbability,
            p.Summary,
            p.CalculatedUtc
        };
    });

    return Results.Ok(result);
});

app.MapPost("/api/admin/seed/teams", async (SeedService seedService, CancellationToken ct) =>
{
    var created = await seedService.SeedTeamsAsync(ct);
    return Results.Ok(new { created });
});

app.MapPost("/api/admin/seed/games/today", async (SeedService seedService, CancellationToken ct) =>
{
    var today = GetSportsLocalDate();
    var created = await seedService.SeedGamesByDateAsync(today, ct);
    return Results.Ok(new { date = today, created });
});

app.MapPost("/api/admin/predictions/today", async (PredictionService predictionService, CancellationToken ct) =>
{
    var today = GetSportsLocalDate();
    var predictions = await predictionService.CalculatePredictionsForDateAsync(today, ct);
    return Results.Ok(new { date = today, count = predictions.Count });
});

app.MapPost("/api/admin/sync/now", async (IServiceProvider sp, CancellationToken ct) =>
{
    using var scope = sp.CreateScope();

    var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
    var predictionService = scope.ServiceProvider.GetRequiredService<PredictionService>();

    var sportsDate = GetSportsLocalDate();
    var dates = new[]
    {
        sportsDate.AddDays(-1),
        sportsDate,
        sportsDate.AddDays(1)
    };

    var seededByDate = new List<object>();

    await seedService.SeedTeamsAsync(ct);

    foreach (var date in dates)
    {
        var createdGames = await seedService.SeedGamesByDateAsync(date, ct);
        var predictions = await predictionService.CalculatePredictionsForDateAsync(date, ct);

        seededByDate.Add(new
        {
            date,
            createdGames,
            predictions = predictions.Count
        });
    }

    return Results.Ok(new
    {
        message = "Sync manual completada.",
        localDate = sportsDate,
        dates = seededByDate
    });
});

app.MapFallbackToFile("index.html");

app.Run();

static DateOnly GetSportsLocalDate()
{
    var madridTz = GetMadridTimeZone();
    var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, madridTz);
    return DateOnly.FromDateTime(localNow);
}

static (DateOnly Start, DateOnly Today, DateOnly End) GetSportsWindow()
{
    var today = GetSportsLocalDate();
    return (today.AddDays(-1), today, today.AddDays(1));
}

static (DateTime StartUtc, DateTime EndUtc) GetUtcRangeForLocalDate(DateOnly localDate)
{
    var madridTz = GetMadridTimeZone();

    var localStart = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
    var localEnd = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

    var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, madridTz);
    var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, madridTz);

    return (startUtc, endUtc);
}

static (DateTime StartUtc, DateTime EndUtc) GetUtcRangeForWindow((DateOnly Start, DateOnly Today, DateOnly End) window)
{
    var madridTz = GetMadridTimeZone();

    var localStart = window.Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
    var localEnd = window.End.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

    var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, madridTz);
    var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, madridTz);

    return (startUtc, endUtc);
}

static DateTime ConvertUtcToMadrid(DateTime utc)
{
    var madridTz = GetMadridTimeZone();

    var normalizedUtc = utc.Kind == DateTimeKind.Utc
        ? utc
        : DateTime.SpecifyKind(utc, DateTimeKind.Utc);

    return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, madridTz);
}

static TimeZoneInfo GetMadridTimeZone()
{
    return TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Romance Standard Time" : "Europe/Madrid");
}