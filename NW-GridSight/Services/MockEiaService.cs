using NW_GridSight.Models;

namespace NW_GridSight.Services
{
    public class MockEiaService : IEiaService
    {
        public Task<List<PowerData>> GetLast24HoursDataAsync()
        {
            var now = DateTime.UtcNow;

            var data = new List<PowerData>();

            for (int i = 0; i < 24; i++)
            {
                data.Add(new PowerData
                {
                    Source = "Hydro",
                    Region = "BPA",
                    GenerationMegawatts = 1000 + i * 10,
                    TimestampUtc = now.AddHours(-i)
                });

                data.Add(new PowerData
                {
                    Source = "Wind",
                    Region = "BPA",
                    GenerationMegawatts = 500 + i * 5,
                    TimestampUtc = now.AddHours(-i)
                });
            }

            return Task.FromResult(data);
        }

        public async Task<List<PowerData>> GetPowerDataSnapshot()
        {
            var data = await GetLast24HoursDataAsync();

            return [.. data
                .GroupBy(x => x.Source)
                .Select(g => g.OrderByDescending(x => x.TimestampUtc).First())];
        }
    }
}
