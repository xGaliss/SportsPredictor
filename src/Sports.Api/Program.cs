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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new { name = "SportsPredictor API", status = "ok" }));

app.MapGet("/api/admin/config/balldontlie", (IOptions<BalldontlieOptions> options) =>
{
    var value = options.Value;
    return Results.Ok(new
    {
        value.BaseUrl,
        HasApiKey = !string.IsNullOrWhiteSpace(value.ApiKey) && !string.Equals(value.ApiKey, "PUT_YOUR_API_KEY_HERE", StringComparison.OrdinalIgnoreCase),
        value.UseMockFallbackData
    });
});

app.MapGet("/api/games/today", async (AppDbContext db, CancellationToken ct) =>
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var start = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var end = start.AddDays(1);

    var teams = await db.Teams.ToDictionaryAsync(x => x.Id, ct);
    var games = await db.Games
        .Where(x => x.GameDateUtc >= start && x.GameDateUtc < end)
        .OrderBy(x => x.GameDateUtc)
        .ToListAsync(ct);

    var result = games.Select(g => new
    {
        g.Id,
        g.ExternalId,
        g.GameDateUtc,
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

app.MapGet("/api/predictions/today", async (AppDbContext db, CancellationToken ct) =>
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var start = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var end = start.AddDays(1);

    var teams = await db.Teams.ToDictionaryAsync(x => x.Id, ct);
    var games = await db.Games
        .Where(x => x.GameDateUtc >= start && x.GameDateUtc < end)
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
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var created = await seedService.SeedGamesByDateAsync(today, ct);
    return Results.Ok(new { date = today, created });
});

app.MapPost("/api/admin/predictions/today", async (PredictionService predictionService, CancellationToken ct) =>
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var predictions = await predictionService.CalculatePredictionsForDateAsync(today, ct);
    return Results.Ok(new { date = today, count = predictions.Count });
});

app.Run();
