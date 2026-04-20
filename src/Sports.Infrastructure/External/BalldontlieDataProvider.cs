using System.Net.Http.Json;
using System.Web;

namespace Sports.Infrastructure.External;

public class BalldontlieDataProvider : IBasketballDataProvider
{
    private readonly HttpClient _httpClient;

    public BalldontlieDataProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<BalldontlieTeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<BalldontlieListResponse<BalldontlieTeamDto>>("/v1/teams", cancellationToken);
        return response?.Data ?? new List<BalldontlieTeamDto>();
    }

    public async Task<IReadOnlyCollection<BalldontlieGameDto>> GetGamesByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var formatted = date.ToString("yyyy-MM-dd");
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["dates[]"] = formatted;
        query["per_page"] = "100";

        var response = await _httpClient.GetFromJsonAsync<BalldontlieListResponse<BalldontlieGameDto>>($"/v1/games?{query}", cancellationToken);
        return response?.Data ?? new List<BalldontlieGameDto>();
    }
}
