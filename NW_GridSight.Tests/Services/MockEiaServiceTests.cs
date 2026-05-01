using NW_GridSight.Services;

namespace NW_GridSight.Tests.Services
{
    public class MockEiaServiceTests
    {
        private readonly MockEiaService _service;

        public MockEiaServiceTests()
        {
            _service = new MockEiaService();
        }

        [Fact]
        public async Task GetPowerDataSnapshot_ReturnsData()
        {
            var result = await _service.GetPowerDataSnapshot();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetPowerDataSnapshot_ContainsPnwRegions()
        {
            var result = await _service.GetPowerDataSnapshot();

            var regions = result.Select(x => x.Region).Distinct().ToList();

            Assert.Contains("Avista Corporation", regions);
            Assert.Contains("PacifiCorp West", regions);
            Assert.Contains("Portland General Electric Company", regions);
        }

        [Fact]
        public async Task GetLast24HoursDataAsync_ReturnsHistoricalData()
        {
            var result = await _service.GetLast24HoursDataAsync();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetPowerDataSnapshot_ReturnsConsistentData()
        {
            // Act
            var result1 = await _service.GetPowerDataSnapshot();
            var result2 = await _service.GetPowerDataSnapshot();

            // Assert - Mock should return consistent data
            Assert.Equal(result1.Count, result2.Count);
        }

        [Fact]
        public async Task GetPowerDataSnapshot_AllTimestampsAreUtc()
        {
            // Act
            var result = await _service.GetPowerDataSnapshot();

            // Assert
            Assert.All(result, item => Assert.Equal(DateTimeKind.Utc, item.TimestampUtc.Kind));
        }

        [Fact]
        public async Task GetLast24HoursDataAsync_ContainsMultipleHours()
        {
            // Act
            var result = await _service.GetLast24HoursDataAsync();

            // Group by hour to verify we have multiple hours of data
            var hourCount = result
                .Select(x => x.TimestampUtc.Hour)
                .Distinct()
                .Count();

            // Assert
            Assert.True(hourCount > 1, "Should have data for multiple hours");
        }

        [Fact]
        public async Task GetLast24HoursDataAsync_DataIsWithinLast24Hours()
        {
            // Act
            var result = await _service.GetLast24HoursDataAsync();
            var now = DateTime.UtcNow;

            // Assert
            Assert.All(result, item =>
            {
                var hoursDiff = (now - item.TimestampUtc).TotalHours;
                Assert.True(hoursDiff >= 0 && hoursDiff <= 24,
                    $"Timestamp {item.TimestampUtc} should be within last 24 hours");
            });
        }
    }
}
