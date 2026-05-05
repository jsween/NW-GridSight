using Microsoft.AspNetCore.Mvc;
using NW_GridSight.Services;

namespace NW_GridSight.Controllers
{
    public class DashboardController(IEiaService eiaService, IDashboardService dashboardService) : Controller
    {
        private readonly IEiaService _eiaService = eiaService;
        private readonly IDashboardService _dashboardService = dashboardService;

        public async Task<IActionResult> Index()
        {
            var latestSnapshot = await _eiaService.GetPowerDataSnapshot();
            var historicalData = await _eiaService.GetLast24HoursDataAsync();

            var vm = _dashboardService.BuildDashboard(latestSnapshot, historicalData);
            return View(vm);
        }
    }
}
