using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NW_GridSight.Models;
using System.Net;

namespace NW_GridSight.Tests.Integration
{
    public class DashboardIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public DashboardIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Dashboard_Index_ReturnsSuccessStatusCode()
        {
            // Arrange
            ResetMocks();
            SetupMockData();

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Dashboard_Index_ReturnsCorrectContentType()
        {
            // Arrange
            ResetMocks();
            SetupMockData();

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");

            // Assert
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task Dashboard_Index_ContainsDashboardTitle()
        {
            // Arrange
            ResetMocks();
            SetupMockData();

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("NW-GridSight Dashboard", content);
        }

        [Fact]
        public async Task Dashboard_Index_DisplaysPowerSourceData()
        {
            // Arrange
            ResetMocks();
            var now = DateTime.UtcNow;
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 1500, TimestampUtc = now },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 800, TimestampUtc = now }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Hydro", content);
            Assert.Contains("Wind", content);
            Assert.Contains("2300 MW", content); // Total generation
        }

        [Fact]
        public async Task Dashboard_Index_HandlesEmptyData()
        {
            // Arrange
            ResetMocks();
            _factory.MockEiaService!
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(new List<PowerData>());

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Dashboard_Index_CallsEiaServiceGetLast24HoursDataAsync()
        {
            // Arrange
            ResetMocks();
            SetupMockData();

            // Act
            await _client.GetAsync("/Dashboard/Index");

            // Assert
            _factory.MockEiaService!.Verify(s => s.GetLast24HoursDataAsync(), Times.Once);
        }

        [Fact]
        public async Task Dashboard_Index_FiltersToPnwRegions()
        {
            // Arrange
            ResetMocks();
            var now = DateTime.UtcNow;
            var mockData = new List<PowerData>
            {
                // PNW regions
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = now },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 400, TimestampUtc = now },
                
                // Non-PNW region (should be filtered out)
                new() { Region = "California ISO", Source = "Solar", GenerationMegawatts = 300, TimestampUtc = now }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();

            // Verify PNW power sources are displayed
            Assert.Contains("Hydro", content);
            Assert.Contains("Wind", content);

            // Verify non-PNW power source is NOT displayed
            Assert.DoesNotContain("Solar", content);

            // Verify total only includes PNW regions (900 MW, not 1200 MW)
            Assert.Contains("900 MW", content);
        }

        [Fact]
        public async Task Dashboard_Index_SplitsLatestAndHistoricalData()
        {
            // Arrange
            ResetMocks();
            var now = DateTime.UtcNow;
            var mockData = new List<PowerData>
            {
                // Latest snapshot
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = now },
                
                // Historical data
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 480, TimestampUtc = now.AddHours(-1) },
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 460, TimestampUtc = now.AddHours(-2) }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            // Current generation should be based on latest snapshot (500 MW)
            Assert.Contains("500 MW", content);
        }

        [Fact]
        public async Task Dashboard_Index_HandlesApiError()
        {
            // Arrange
            ResetMocks();
            _factory.MockEiaService!
                .Setup(s => s.GetLast24HoursDataAsync())
                .ThrowsAsync(new HttpRequestException("API unavailable"));

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");

            // Assert
            // Should still return 200 with error message in view model
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Unable to retrieve live grid data", content);
        }

        [Fact]
        public async Task Home_Index_ReturnsSuccess()
        {
            // Arrange
            ResetMocks();
            SetupMockData();

            // Act
            var response = await _client.GetAsync("/");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Theory]
        [InlineData("/Dashboard/Index")]
        [InlineData("/")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            ResetMocks();
            SetupMockData();

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        private void ResetMocks()
        {
            _factory.MockEiaService?.Reset();
            _factory.MockCache?.Reset();

            // Re-setup default cache behavior after reset
            object? nullValue = null;
            _factory.MockCache?.Setup(c => c.TryGetValue(It.IsAny<object>(), out nullValue))
                .Returns(false);

            _factory.MockCache?.Setup(c => c.CreateEntry(It.IsAny<object>()))
                .Returns((object key) =>
                {
                    var mockEntry = new Mock<ICacheEntry>();
                    mockEntry.SetupProperty(e => e.Value);
                    mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                    // Don't setup Key property - it's read-only
                    mockEntry.SetupGet(e => e.Key).Returns(key);
                    return mockEntry.Object;
                });
        }

        private void SetupMockData()
        {
            var now = DateTime.UtcNow;
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = now }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);
        }
    }
}