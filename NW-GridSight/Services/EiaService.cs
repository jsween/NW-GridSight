using Microsoft.Extensions.Options;
using NW_GridSight.Configuration;
using NW_GridSight.Extensions;
using NW_GridSight.Mappers;
using NW_GridSight.Models;

namespace NW_GridSight.Services
{

    public class EiaService(
        HttpClient httpClient,
        IOptions<EiaApiOptions> options,
        ILogger<EiaService> logger,
        IClock clock) : IEiaService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly EiaApiOptions _options = options.Value;
        private readonly ILogger<EiaService> _logger = logger;
        private readonly IClock _clock = clock;

        public async Task<List<PowerData>> GetPowerDataSnapshot()
        {
            var data = await GetLast24HoursDataAsync();

            return [.. data
                .GroupBy(x => x.Source)
                .Select(g => g.OrderByDescending(x => x.TimestampUtc).First())];
        }

        public async Task<List<PowerData>> GetLast24HoursDataAsync()
        {
            DateTime startUtc = _clock.UtcNow.AddHours(-24);
            DateTime endUtc = _clock.UtcNow;

            string? requestUrl = BuildRequestUrl(startUtc, endUtc);

            var response = await _httpClient.GetAsync(requestUrl);
            string? json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("EIA API returned a sattus code {StatusCode}\nBody: {body}", response.StatusCode, json);
                throw new HttpRequestException(
                    $"EIA API request failed with status code: {response.StatusCode}",
                    null,
                    response.StatusCode);
            }

            return EiaPowerDataMapper.MapResponse(json);
        }

        internal string BuildRequestUrl(DateTime startUtc, DateTime endUtc)
        {
            string start = startUtc.ToEiaString();
            string end = endUtc.ToEiaString();

            return $"{_options.BaseUrl}electricity/rto/fuel-type-data/data/?" +
                $"api_key={_options.ApiKey}" +
                $"&frequency=hourly" +
                $"&data[0]=value" +
                $"&facets[respondent][]=AVA" +
                $"&facets[respondent][]=BPA" +
                $"&facets[respondent][]=PGE" +
                $"&facets[respondent][]=PACW" +
                $"&sort[0][column]=period" +
                $"&sort[0][direction]=desc" +
                $"&start={start}" +
                $"&end={end}" +
                $"&offset=0" +
                $"&length=5000";
        }
    }
}



