using NW_GridSight.Models;
using System.Globalization;
using System.Text.Json;

namespace NW_GridSight.Mappers
{
    public static class EiaPowerDataMapper
    {
        public static PowerData MapToPowerData(JsonElement item)
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
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var value)
                    ? value
                    : 0;

            var periodString = item.TryGetProperty("period", out var periodProp)
                ? periodProp.GetString()
                : null;

            var timestampUtc = DateTime.TryParseExact(
                periodString,
                "yyyy-MM-dd'T'HH",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp)
                    ? DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
                    : DateTime.UtcNow;

            return new PowerData
            {
                Region = region,
                Source = NormalizeSource(source),
                GenerationMegawatts = generationMegawatts,
                TimestampUtc = timestampUtc
            };
        }

        public static List<PowerData> MapResponse(string json)
        {
            var results = new List<PowerData>();

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("response", out var responseElement) ||
                !responseElement.TryGetProperty("data", out var dataArray) ||
                dataArray.ValueKind != JsonValueKind.Array)
            {
                return results;
            }

            foreach (var item in dataArray.EnumerateArray())
            {
                results.Add(MapToPowerData(item));
            }

            return results;
        }

        private static string NormalizeSource(string? source)
        {
            if (string.IsNullOrEmpty(source))
                return "Unknown";
            return source.Trim().ToLowerInvariant() switch
            {
                "hydro" => "Hydro",
                "batter" => "Battery Storage",
                "coal" => "Coal",
                "natural gas" => "Natural Gas",
                "nuclear" => "Nuclear",
                _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(source.Trim().ToLowerInvariant())
            };
        }
    }
}
