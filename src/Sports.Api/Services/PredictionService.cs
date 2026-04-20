using Sports.Api.Helpers;
using Microsoft.EntityFrameworkCore;
using Sports.Api.Data;
using Sports.Domain.Entities;

namespace Sports.Api.Services;

public class PredictionService
{
    private readonly AppDbContext _dbContext;

    public PredictionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Prediction>> CalculatePredictionsForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var (start, end) = DateRangeHelper.GetUtcRangeForLocalDate(date);

        var games = await _dbContext.Games
            .Where(x => x.GameDateUtc >= start && x.GameDateUtc < end)
            .OrderBy(x => x.GameDateUtc)
            .ToListAsync(cancellationToken);

        var predictions = new List<Prediction>();

        foreach (var game in games)
        {
            var homeRating = await BuildTeamRatingAsync(game.HomeTeamId, cancellationToken) + 3.0m;
            var awayRating = await BuildTeamRatingAsync(game.AwayTeamId, cancellationToken);

            var total = homeRating + awayRating;
            if (total <= 0) total = 1;

            var homeProbability = Math.Round((homeRating / total) * 100m, 2);
            var awayProbability = Math.Round(100m - homeProbability, 2);

            var prediction = await _dbContext.Predictions.FirstOrDefaultAsync(x => x.GameId == game.Id, cancellationToken);
            if (prediction is null)
            {
                prediction = new Prediction { GameId = game.Id };
                _dbContext.Predictions.Add(prediction);
            }

            prediction.HomeRating = Math.Round(homeRating, 2);
            prediction.AwayRating = Math.Round(awayRating, 2);
            prediction.HomeWinProbability = homeProbability;
            prediction.AwayWinProbability = awayProbability;
            prediction.CalculatedUtc = DateTime.UtcNow;
            prediction.Summary = homeProbability >= awayProbability
                ? $"Ventaja local por forma reciente y bonus de localía. {homeProbability}% vs {awayProbability}%."
                : $"Visitante mejor por forma reciente. {awayProbability}% vs {homeProbability}%.";

            predictions.Add(prediction);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return predictions;
    }

    private async Task<decimal> BuildTeamRatingAsync(int teamId, CancellationToken cancellationToken)
    {
        var recentGames = await _dbContext.Games
            .Where(x => x.IsCompleted && (x.HomeTeamId == teamId || x.AwayTeamId == teamId))
            .OrderByDescending(x => x.GameDateUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (recentGames.Count == 0)
        {
            return 50m;
        }

        decimal wins = 0;
        decimal pointsFor = 0;
        decimal pointsAgainst = 0;

        foreach (var game in recentGames)
        {
            var isHome = game.HomeTeamId == teamId;
            var scored = isHome ? game.HomeScore ?? 0 : game.AwayScore ?? 0;
            var allowed = isHome ? game.AwayScore ?? 0 : game.HomeScore ?? 0;

            pointsFor += scored;
            pointsAgainst += allowed;

            if (scored > allowed)
            {
                wins += 1;
            }
        }

        var avgFor = pointsFor / recentGames.Count;
        var avgAgainst = pointsAgainst / recentGames.Count;
        var winRate = (wins / recentGames.Count) * 100m;

        return (winRate * 0.6m) + (avgFor * 0.3m) - (avgAgainst * 0.2m);
    }
}
