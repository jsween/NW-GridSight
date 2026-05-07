using NW_GridSight.ViewModels;

namespace NW_GridSight.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> BuildDashboardAsync();
    }
}
