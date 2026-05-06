using NW_GridSight.Models;
using NW_GridSight.ViewModels;

namespace NW_GridSight.Services
{
    public class DashboardService(IClock clock) : IDashboardService
    {
        private readonly IClock _clock = clock;

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
    }
}
