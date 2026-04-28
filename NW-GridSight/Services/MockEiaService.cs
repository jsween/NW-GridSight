using NW_GridSight.Models;

namespace NW_GridSight.Services
{
    public class MockEiaService : IEiaService
    {
        public Task<List<PowerData>> GetCurrentPowerDataAsync()
        {
            var mockData = new List<PowerData>
            {
                new() { Region = "Northwest", Source = "Hydro", GenerationMegawatts = 5000, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Northwest", Source = "Wind", GenerationMegawatts = 2000, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Northwest", Source = "Solar", GenerationMegawatts = 1500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Northwest", Source = "Natural Gas", GenerationMegawatts = 3000, TimestampUtc = DateTime.UtcNow }
            };
            return Task.FromResult(mockData);
        }
    }
}
