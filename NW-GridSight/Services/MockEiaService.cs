using NW_GridSight.Models;

namespace NW_GridSight.Services
{
    public class MockEiaService : IEiaService
    {
        public Task<List<PowerData>> GetCurrentPowerDataAsync()
        {
            var mockData = new List<PowerData>
            {
                new() { Region = "Northwest", Source = "Hydro", GenerationMegaWatts = 5000, TimeStampUtc = DateTime.UtcNow },
                new() { Region = "Northwest", Source = "Wind", GenerationMegaWatts = 2000, TimeStampUtc = DateTime.UtcNow },
                new() { Region = "Northwest", Source = "Solar", GenerationMegaWatts = 1500, TimeStampUtc = DateTime.UtcNow },
                new() { Region = "Northwest", Source = "Natural Gas", GenerationMegaWatts = 3000, TimeStampUtc = DateTime.UtcNow }
            };
            return Task.FromResult(mockData);
        }
    }
}
