using Microsoft.AspNetCore.Mvc;
using NW_GridSight.Services;
using NW_GridSight.ViewModels;

namespace NW_GridSight.Controllers
{
    public class DashboardController(IEiaService eiaService) : Controller
    {
        private readonly IEiaService _eiaService = eiaService;

        public async Task<IActionResult> Index()
        {
            var powerData = await _eiaService.GetCurrentPowerDataAsync();

            var vm = new DashboardViewModel
            {
                PowerSources = powerData,
                LastUpdatedUtc = DateTime.UtcNow
            };

            return View(vm);
        }
    }
}
