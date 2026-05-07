using Microsoft.Extensions.Caching.Memory;
using NW_GridSight.Models;
using NW_GridSight.ViewModels;

namespace NW_GridSight.Services
{
    public class DashboardService(IClock clock, IEiaService eiaService, IMemoryCache cache, ILogger<DashboardService> logger) : IDashboardService
    {
        private const string DashboardCacheKey = "LastSuccessfulDashboard";

        private readonly IClock _clock = clock;
        private readonly IEiaService _eiaService = eiaService;
        private readonly IMemoryCache _cache = cache;
        private readonly ILogger<DashboardService> _logger = logger;

        public DashboardViewModel BuildDashboard(List<PowerData> latestSnapshot, List<PowerData> historicalData)
        {
            var pnwRegions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Avista Corporation",
                "PacifiCorp West",
                "Portland General Electric Company"
            };

            var filteredSnapshot = latestSnapshot
                .Where(x => pnwRegions.Contains(x.Region))
                .ToList();

            var summaries = filteredSnapshot
                .GroupBy(x => x.Source)
                .Select(g => new PowerSourceSummary
                {
                    PowerSource = g.Key,
                    GenerationMegawatts = (int)g.Sum(x => x.GenerationMegawatts)
                })
                .OrderByDescending(s => s.GenerationMegawatts)
                .ToList();

            var totalGeneration = summaries.Sum(x => x.GenerationMegawatts);

            foreach (var summary in summaries)
            {
                summary.Percentage = totalGeneration == 0
                    ? 0
                    : (int)((double)summary.GenerationMegawatts / totalGeneration * 100);
            }

            var hydro = summaries
                .FirstOrDefault(x => x.PowerSource == "Hydro")?.GenerationMegawatts ?? 0;

            var hydroPercent = totalGeneration == 0
                ? 0
                : (int)((double)hydro / totalGeneration * 100);

            return new DashboardViewModel
            {
                LatestSnapshot = filteredSnapshot,
                HistoricalData = historicalData,
                PowerSourceSummaries = summaries,
                TotalGenerationMegawatts = totalGeneration,
                LastUpdatedUtc = _clock.UtcNow,
                HydroPercentage = hydroPercent,
                TopSource = summaries.FirstOrDefault()?.PowerSource ?? "Unknown"
            };
        }

        public async Task<DashboardViewModel> BuildDashboardAsync()
        {
            try
            {
                var powerData = await _eiaService.GetLast24HoursDataAsync();

                // Get the most recent timestamp to identify the latest snapshot
                var latestTimestamp = powerData.Max(x => x.TimestampUtc);

                // Split into latest snapshot (most recent hour) and historical data
                var latestSnapshot = powerData
                    .Where(x => x.TimestampUtc == latestTimestamp)
                    .ToList();

                var historicalData = powerData
                    .Where(x => x.TimestampUtc < latestTimestamp)
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();

                var viewModel = BuildDashboard(latestSnapshot, historicalData);

                _cache.Set(
                    DashboardCacheKey,
                    viewModel,
                    TimeSpan.FromHours(6)
                );

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build dashboard from live data.");

                // Try to return cached dashboard if available
                if (_cache.TryGetValue(DashboardCacheKey, out DashboardViewModel? cachedDashboard) && cachedDashboard != null)
                {
                    _logger.LogInformation("Returning cached dashboard due to error.");
                    return cachedDashboard;
                }

                // If no cache available, create a skeleton
                return new DashboardViewModel
                {
                    HasError = true,
                    ErrorMessage = $"Unable to retrieve live grid data\n{ex.Message}",
                    HistoricalData = [],
                    PowerSourceSummaries = []
                };
            }
        }
    }
}
