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

        public async Task<List<PowerData>> GetCurrentPowerDataAsync()
        {
            DateTime endUtc = DateTime.UtcNow;
            String end = endUtc.ToEiaString();
            DateTime startUtc = endUtc.AddHours(-24);
            String start = startUtc.ToEiaString();

            String? requestUrl = BuildRequestUrl();

            _logger.LogInformation("Calling EIA API with URL: {requestUrl}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);

            String? json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("EIA API returned a status code: {StatusCode}\nBody: {body}", response.StatusCode, json);
                throw new Exception($"EIA API request failed with status code: {response.StatusCode}");
            }

            return EiaPowerDataMapper.MapResponse(json);
        }

        internal string BuildRequestUrl()
        {
            DateTime endUtc = _clock.UtcNow;
            DateTime startUtc = endUtc.AddHours(-24);

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
                $"&length=25";
        }
    }
}



