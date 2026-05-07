using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NW_GridSight.Models;
using NW_GridSight.Services;
using NW_GridSight.ViewModels;

namespace NW_GridSight.Tests.Services
{
    public class DashboardServiceAsyncTests
    {
        private readonly Mock<IClock> _mockClock;
        private readonly Mock<IEiaService> _mockEiaService;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<ILogger<DashboardService>> _mockLogger;
        private readonly DashboardService _service;

        public DashboardServiceAsyncTests()
        {
            _mockClock = new Mock<IClock>();
            _mockEiaService = new Mock<IEiaService>();
            _mockCache = new Mock<IMemoryCache>();
            _mockLogger = new Mock<ILogger<DashboardService>>();

            _service = new DashboardService(
                _mockClock.Object,
                _mockEiaService.Object,
                _mockCache.Object,
                _mockLogger.Object);
        }

        #region BuildDashboardAsync - Success Cases

        [Fact]
        public async Task BuildDashboardAsync_FetchesDataFromEiaService()
        {
            // Arrange
            var mockData = CreateMockPowerData();
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);
            SetupCacheMock();

            // Act
            await _service.BuildDashboardAsync();

            // Assert
            _mockEiaService.Verify(s => s.GetLast24HoursDataAsync(), Times.Once);
        }

        [Fact]
        public async Task BuildDashboardAsync_SplitsDataIntoLatestAndHistorical()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = now },
                new() { Region = "Avista Corporation", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = now },
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 480, TimestampUtc = now.AddHours(-1) },
                new() { Region = "Avista Corporation", Source = "Wind", GenerationMegawatts = 290, TimestampUtc = now.AddHours(-2) }
            };

            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);
            SetupCacheMock();

            // Act
            var result = await _service.BuildDashboardAsync();

            // Assert
            Assert.Equal(2, result.LatestSnapshot.Count); // Only the latest timestamp
            Assert.Equal(2, result.HistoricalData.Count); // Older data points
            Assert.All(result.LatestSnapshot, item => Assert.Equal(now, item.TimestampUtc));
        }

        [Fact]
        public async Task BuildDashboardAsync_ReturnsViewModelWithCorrectData()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 600, TimestampUtc = now },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 400, TimestampUtc = now }
            };

            _mockClock.Setup(c => c.UtcNow).Returns(now);
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);
            SetupCacheMock();

            // Act
            var result = await _service.BuildDashboardAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1000, result.TotalGenerationMegawatts);
            Assert.Equal("Hydro", result.TopSource);
            Assert.Equal(60, result.HydroPercentage);
            Assert.Equal(now, result.LastUpdatedUtc);
        }

        [Fact]
        public async Task BuildDashboardAsync_OrdersHistoricalDataByTimestamp()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = now },
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 450, TimestampUtc = now.AddHours(-3) },
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 480, TimestampUtc = now.AddHours(-1) },
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 470, TimestampUtc = now.AddHours(-2) }
            };

            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);
            SetupCacheMock();

            // Act
            var result = await _service.BuildDashboardAsync();

            // Assert
            Assert.Equal(3, result.HistoricalData.Count);
            Assert.True(result.HistoricalData[0].TimestampUtc < result.HistoricalData[1].TimestampUtc);
            Assert.True(result.HistoricalData[1].TimestampUtc < result.HistoricalData[2].TimestampUtc);
        }

        #endregion

        #region BuildDashboardAsync - Caching

        [Fact]
        public async Task BuildDashboardAsync_CachesSuccessfulResult()
        {
            // Arrange
            var mockData = CreateMockPowerData();
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            object? capturedValue = null;
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
                .Returns((object key) =>
                {
                    var mockEntry = new Mock<ICacheEntry>();
                    mockEntry.SetupProperty(e => e.Value);
                    mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                    mockEntry.Setup(e => e.Dispose()).Callback(() => capturedValue = mockEntry.Object.Value);
                    return mockEntry.Object;
                });

            // Act
            var result = await _service.BuildDashboardAsync();

            // Assert
            Assert.NotNull(capturedValue);
            Assert.IsType<DashboardViewModel>(capturedValue);
            _mockCache.Verify(c => c.CreateEntry("LastSuccessfulDashboard"), Times.Once);
        }

        [Fact]
        public async Task BuildDashboardAsync_SetsCacheExpirationTo6Hours()
        {
            // Arrange
            var mockData = CreateMockPowerData();
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            TimeSpan? capturedExpiration = null;
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
                .Returns((object key) =>
                {
                    var mockEntry = new Mock<ICacheEntry>();
                    mockEntry.SetupProperty(e => e.Value);
                    mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                    mockEntry.Setup(e => e.Dispose()).Callback(() =>
                        capturedExpiration = mockEntry.Object.AbsoluteExpirationRelativeToNow);
                    return mockEntry.Object;
                });

            // Act
            await _service.BuildDashboardAsync();

            // Assert
            Assert.Equal(TimeSpan.FromHours(6), capturedExpiration);
        }

        #endregion

        #region BuildDashboardAsync - Error Handling

        [Fact]
        public async Task BuildDashboardAsync_ReturnsCachedDashboard_WhenEiaServiceFails()
        {
            // Arrange
            var cachedViewModel = new DashboardViewModel
            {
                TotalGenerationMegawatts = 999,
                TopSource = "CachedSource"
            };

            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync())
                .ThrowsAsync(new HttpRequestException("API unavailable"));

            _mockCache.Setup(c => c.TryGetValue("LastSuccessfulDashboard", out It.Ref<object?>.IsAny))
                .Returns((object key, out object? value) =>
                {
                    value = cachedViewModel;
                    return true;
                });

            // Act
            var result = await _service.BuildDashboardAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(999, result.TotalGenerationMegawatts);
            Assert.Equal("CachedSource", result.TopSource);
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Returning cached dashboard")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BuildDashboardAsync_LogsError_WhenEiaServiceFails()
        {
            // Arrange
            var exception = new HttpRequestException("API unavailable");
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ThrowsAsync(exception);

            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
                .Returns(false);

            // Act
            var result = await _service.BuildDashboardAsync();

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to build dashboard")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BuildDashboardAsync_ReturnsErrorViewModel_WhenNoCacheAvailable()
        {
            // Arrange
            var exception = new HttpRequestException("API unavailable");
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ThrowsAsync(exception);

            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
                .Returns(false);

            // Act
            var result = await _service.BuildDashboardAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.HasError);
            Assert.Contains("Unable to retrieve live grid data", result.ErrorMessage);
            Assert.Contains("API unavailable", result.ErrorMessage);
            Assert.Empty(result.HistoricalData);
            Assert.Empty(result.PowerSourceSummaries);
        }

        [Fact]
        public async Task BuildDashboardAsync_DoesNotCache_WhenExceptionOccurs()
        {
            // Arrange
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync())
                .ThrowsAsync(new HttpRequestException("API unavailable"));

            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object?>.IsAny))
                .Returns(false);

            // Act
            await _service.BuildDashboardAsync();

            // Assert
            _mockCache.Verify(c => c.CreateEntry(It.IsAny<object>()), Times.Never);
        }

        #endregion

        #region BuildDashboardAsync - Edge Cases

        [Fact]
        public async Task BuildDashboardAsync_HandlesEmptyDataSet()
        {
            // Arrange
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(new List<PowerData>());
            SetupCacheMock();

            // Act
            var result = await _service.BuildDashboardAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.LatestSnapshot);
            Assert.Empty(result.HistoricalData);
            Assert.Empty(result.PowerSourceSummaries);
            Assert.Equal(0, result.TotalGenerationMegawatts);
        }

        [Fact]
        public async Task BuildDashboardAsync_HandlesSingleTimestamp()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = now },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = now }
            };

            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);
            SetupCacheMock();

            // Act
            var result = await _service.BuildDashboardAsync();

            // Assert
            Assert.Equal(2, result.LatestSnapshot.Count);
            Assert.Empty(result.HistoricalData);
        }

        #endregion

        #region Helper Methods

        private List<PowerData> CreateMockPowerData()
        {
            var now = DateTime.UtcNow;
            return new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = now },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = now }
            };
        }

        private void SetupCacheMock()
        {
            _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
                .Returns((object key) =>
                {
                    var mockEntry = new Mock<ICacheEntry>();
                    mockEntry.SetupProperty(e => e.Value);
                    mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                    return mockEntry.Object;
                });
        }

        #endregion
    }
}