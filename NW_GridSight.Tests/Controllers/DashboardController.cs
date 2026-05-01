using Microsoft.AspNetCore.Mvc;
using Moq;
using NW_GridSight.Controllers;
using NW_GridSight.Models;
using NW_GridSight.Services;
using NW_GridSight.ViewModels;

namespace NW_GridSight.Tests.Controllers
{
    public class DashboardControllerTests
    {
        private readonly Mock<IEiaService> _mockService;
        private readonly DashboardController _controller;

        public DashboardControllerTests()
        {
            _mockService = new Mock<IEiaService>();
            _controller = new DashboardController(_mockService.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithViewModel()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task Index_FiltersToPnwRegionsOnly()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "California ISO", Source = "Solar", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(2, model.LatestSnapshot.Count);
            Assert.DoesNotContain(model.LatestSnapshot, x => x.Region == "California ISO");
        }

        [Fact]
        public async Task Index_CalculatesTotalGenerationCorrectly()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.Equal(800, model?.TotalGenerationMegawatts);
        }

        [Fact]
        public async Task Index_GroupsByPowerSource()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Hydro", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(2, model.PowerSourceSummaries.Count);

            var hydroSummary = model.PowerSourceSummaries.First(x => x.PowerSource == "Hydro");
            Assert.Equal(800, hydroSummary.GenerationMegawatts);
        }

        [Fact]
        public async Task Index_CalculatesHydroPercentageCorrectly()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 600, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 400, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.Equal(60, model?.HydroPercentage);
        }

        [Fact]
        public async Task Index_IdentifiesTopSource()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 800, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.Equal("Hydro", model?.TopSource);
        }

        [Fact]
        public async Task Index_HandlesEmptyData()
        {
            // Arrange
            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(new List<PowerData>());
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(new List<PowerData>());

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.NotNull(model);
            Assert.Empty(model.LatestSnapshot);
            Assert.Equal(0, model.TotalGenerationMegawatts);
        }
    }
}