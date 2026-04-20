using NW_GridSight.Models;

namespace NW_GridSight.ViewModels
{
    public class DashboardViewModel
    {
        public List<PowerData> PowerSources { get; set; } = new();
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}
