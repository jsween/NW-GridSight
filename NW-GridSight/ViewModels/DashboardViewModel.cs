using NW_GridSight.Models;

namespace NW_GridSight.ViewModels
{
    public class DashboardViewModel
    {
        public List<PowerData> LatestSnapshot { get; set; } = [];
        public List<PowerData> HistoricalData { get; set; } = [];
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        public List<PowerSourceSummary> PowerSourceSummaries { get; set; } = [];

        public int TotalGenerationMegawatts { get; set; }
        public int HydroPercentage { get; set; }
        public string? TopSource { get; set; }
    }
}
