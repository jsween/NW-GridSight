using NW_GridSight.Models;
using NW_GridSight.ViewModels;

namespace NW_GridSight.Services
{
    public interface IDashboardService
    {
        DashboardViewModel BuildDashboard(List<PowerData> latestSnapshot, List<PowerData> historicalData);
    }
}
