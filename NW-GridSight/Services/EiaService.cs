using Microsoft.Extensions.Options;
using NW_GridSight.Configuration;
using NW_GridSight.Models;
using System.Text.Json;

namespace NW_GridSight.Services
{
    public class EiaService(HttpClient httpClient, IOptions<EiaApiOptions> options) : IEiaService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly EiaApiOptions _options = options.Value;

        public async Task<List<PowerData>> GetCurrentPowerDataAsync()
        {
            //var requestUrl = $"{_options.BaseUrl}/current/power?api_key={_options.ApiKey}";
            var requestUrl = $"{_options.BaseUrl}electricity/rto/fuel-type-data/data/?" +
                $"api_key={_options.ApiKey}" +
                $"&frequency=hourly" +
                $"&data[0]=value" +
                $"&facets[respondent][]=AVA" +
                $"&facets[respondent][]=BPA" +
                $"&facets[respondent][]=PGE" +
                $"&facets[respondent][]=PACW" +
                $"&sort[0][column]=period" +
                $"&sort[0][direction]=desc" +
                $"&start=2026-04-26T00" +
                $"&end=2026-04-27T23" +
                $"&offset=0" +
                $"&length=25";

            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"EIA API request failed with status code: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var results = new List<PowerData>();

            if (doc.RootElement.TryGetProperty("response", out var responseElement) &&
                responseElement.TryGetProperty("data", out var dataArray) &&
                dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataArray.EnumerateArray())
                {

                    var region = item.TryGetProperty("respondent-name", out var regionProp)
                        ? regionProp.GetString() ?? "Unknown"
                        : "Unknown";

                    var source = item.TryGetProperty("type-name", out var sourceProp)
                        ? sourceProp.GetString() ?? "Unknown"
                        : "Unknown";

                    var valueText = item.TryGetProperty("value", out var valueProp)
                        ? valueProp.GetString()
                        : null;

                    var generationMegawatts = double.TryParse(
                        valueText,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var value)
                            ? value
                            : 0;

                    var periodString = item.TryGetProperty("period", out var periodProp)
                        ? periodProp.GetString()
                        : null;

                    var timestampUtc = DateTime.TryParseExact(
                        periodString,
                        "yyyy-MM-dd'T'HH",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal,
                        out var timestamp)
                            ? timestamp
                            : DateTime.UtcNow;

                    var powerData = new PowerData
                    {
                        Region = region,
                        Source = source,
                        GenerationMegawatts = generationMegawatts,
                        TimestampUtc = timestampUtc
                    };

                    results.Add(powerData);
                }
            }

            return results ?? [];
        }
    }
}
