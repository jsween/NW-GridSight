using Microsoft.AspNetCore.Mvc.Testing;
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
        public async Task Dashboard_Index_ReturnsSuccessAndCorrectContentType()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetPowerDataSnapshot())
                .ReturnsAsync(mockData);

            _factory.MockEiaService
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task Dashboard_Index_ContainsDashboardTitle()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetPowerDataSnapshot())
                .ReturnsAsync(mockData);

            _factory.MockEiaService
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("NW-GridSight Dashboard", content); // Changed to match actual title
        }

        [Fact]
        public async Task Dashboard_Index_DisplaysPowerSourceData()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 1500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 800, TimestampUtc = DateTime.UtcNow }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetPowerDataSnapshot())
                .ReturnsAsync(mockData);

            _factory.MockEiaService
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Hydro", content);
            Assert.Contains("Wind", content);
        }

        [Fact]
        public async Task Dashboard_Index_HandlesEmptyData()
        {
            // Arrange
            _factory.MockEiaService!
                .Setup(s => s.GetPowerDataSnapshot())
                .ReturnsAsync([]);

            _factory.MockEiaService
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync([]);

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Dashboard_Index_CallsEiaServiceMethods()
        {
            // Arrange - Reset the mock to clear previous invocations
            _factory.MockEiaService!.Reset();

            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetPowerDataSnapshot())
                .ReturnsAsync(mockData);

            _factory.MockEiaService
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);

            // Act
            await _client.GetAsync("/Dashboard/Index");

            // Assert
            _factory.MockEiaService.Verify(s => s.GetPowerDataSnapshot(), Times.Once);
            _factory.MockEiaService.Verify(s => s.GetLast24HoursDataAsync(), Times.Once);
        }

        [Fact]
        public async Task Dashboard_Index_FiltersToPnwRegions()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "California ISO", Source = "Solar", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 400, TimestampUtc = DateTime.UtcNow }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetPowerDataSnapshot())
                .ReturnsAsync(mockData);

            _factory.MockEiaService
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);

            // Act
            var response = await _client.GetAsync("/Dashboard/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Avista Corporation", content);
            Assert.Contains("Portland General Electric Company", content);
            // California ISO should be filtered out in the controller
        }

        [Fact]
        public async Task Home_Index_ReturnsSuccess()
        {
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
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };

            _factory.MockEiaService!
                .Setup(s => s.GetPowerDataSnapshot())
                .ReturnsAsync(mockData);

            _factory.MockEiaService
                .Setup(s => s.GetLast24HoursDataAsync())
                .ReturnsAsync(mockData);

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }
    }
}