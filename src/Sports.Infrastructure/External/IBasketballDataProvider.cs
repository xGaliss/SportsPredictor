namespace Sports.Infrastructure.External;

public interface IBasketballDataProvider
{
    Task<IReadOnlyCollection<BalldontlieTeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BalldontlieGameDto>> GetGamesByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
}
