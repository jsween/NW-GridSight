using NW_GridSight.Models;

namespace NW_GridSight.Services
{
    public interface IEiaService
    {
        Task<List<PowerData>> GetCurrentPowerDataAsync();
    }
}
