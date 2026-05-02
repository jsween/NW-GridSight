using Microsoft.AspNetCore.Mvc;
using NW_GridSight.Models;
using NW_GridSight.Services;
using NW_GridSight.ViewModels;

namespace NW_GridSight.Controllers
{
    public class DashboardController(IEiaService eiaService) : Controller
    {
        private readonly IEiaService _eiaService = eiaService;

        public async Task<IActionResult> Index()
        {
            var pnwRegions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Avista Corporation",
                "PacifiCorp West",
                "Portland General Electric Company"
            };

            var SourceOrder = new List<string>
            {
                "Hydro",
                "Natural Gas",
                "Wind",
                "Solar",
                "Nuclear",
                "Coal",
                "Other"
            };

            List<PowerData> powerData = await _eiaService.GetPowerDataSnapshot();

            List<PowerData> filteredData = [.. powerData
                .Where(x => pnwRegions.Contains(x.Region))];

            var historicalData = await _eiaService.GetLast24HoursDataAsync();
            var totalGeneration = filteredData.Sum(x => (int)x.GenerationMegawatts);

            List<PowerSourceSummary> summaries = [.. filteredData
                .GroupBy(x => x.Source)
                .Select(g =>
                {
                    var sourceTotal = g.Sum(x => x.GenerationMegawatts);

                    var percentage = totalGeneration > 0
                        ? (int)Math.Round((double)sourceTotal / totalGeneration * 100)
                        : 0;

                    return new PowerSourceSummary
                    {
                        PowerSource = g.Key,
                        GenerationMegawatts = (int)sourceTotal,
                        Percentage = percentage
                    };
                })
                .OrderByDescending(x => x.GenerationMegawatts)];

            var topSource = summaries
                .OrderByDescending(x => x.GenerationMegawatts)
                .FirstOrDefault()?.PowerSource ?? "Unknown";

            var hydro = summaries.FirstOrDefault(x => x.PowerSource.Equals("Hydro", StringComparison.OrdinalIgnoreCase))?.GenerationMegawatts ?? 0;

            var hydroPercent = totalGeneration > 0
                ? (int)((double)hydro / totalGeneration * 100)
                : 0;

            var vm = new DashboardViewModel
            {

                LatestSnapshot = filteredData,
                HistoricalData = historicalData,

                PowerSourceSummaries = summaries,
                TotalGenerationMegawatts = filteredData.Sum(x => (int)x.GenerationMegawatts),
                LastUpdatedUtc = DateTime.UtcNow,
                HydroPercentage = hydroPercent,
                TopSource = topSource
            };

            return View(vm);
        }
    }
}
