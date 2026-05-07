using Microsoft.AspNetCore.Mvc;
using NW_GridSight.Services;

namespace NW_GridSight.Controllers
{
    public class DashboardController(IDashboardService dashboardService) : Controller
    {
        private readonly IDashboardService _dashboardService = dashboardService;

        public async Task<IActionResult> Index()
        {
            var vm = await _dashboardService.BuildDashboardAsync();
            return View(vm);
        }
    }
}
