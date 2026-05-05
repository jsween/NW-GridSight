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
        private readonly Mock<IEiaService> _mockEiaService;
        private readonly Mock<IDashboardService> _mockDashboardService;
        private readonly DashboardController _controller;

        public DashboardControllerTests()
        {
            _mockEiaService = new Mock<IEiaService>();
            _mockDashboardService = new Mock<IDashboardService>();
            _controller = new DashboardController(_mockEiaService.Object, _mockDashboardService.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithViewModel()
        {
            // Arrange
            var mockSnapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow }
            };
            var mockHistorical = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 450, TimestampUtc = DateTime.UtcNow.AddHours(-1) }
            };

            var expectedViewModel = new DashboardViewModel
            {
                LatestSnapshot = mockSnapshot,
                TotalGenerationMegawatts = 800,
                HydroPercentage = 62,
                TopSource = "Hydro",
                PowerSourceSummaries = new List<PowerSourceSummary>()
            };

            _mockEiaService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockSnapshot);
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockHistorical);
            _mockDashboardService.Setup(s => s.BuildDashboard(mockSnapshot, mockHistorical)).Returns(expectedViewModel);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            Assert.NotNull(model);
            _mockDashboardService.Verify(s => s.BuildDashboard(mockSnapshot, mockHistorical), Times.Once);
        }

        [Fact]
        public async Task Index_CallsEiaServiceMethods()
        {
            // Arrange
            var mockSnapshot = new List<PowerData>();
            var mockHistorical = new List<PowerData>();
            var expectedViewModel = new DashboardViewModel();

            _mockEiaService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockSnapshot);
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockHistorical);
            _mockDashboardService.Setup(s => s.BuildDashboard(It.IsAny<List<PowerData>>(), It.IsAny<List<PowerData>>())).Returns(expectedViewModel);

            // Act
            await _controller.Index();

            // Assert
            _mockEiaService.Verify(s => s.GetPowerDataSnapshot(), Times.Once);
            _mockEiaService.Verify(s => s.GetLast24HoursDataAsync(), Times.Once);
        }

        [Fact]
        public async Task Index_PassesDataToDashboardService()
        {
            // Arrange
            var mockSnapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };
            var mockHistorical = new List<PowerData>
            {
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow.AddHours(-2) }
            };
            var expectedViewModel = new DashboardViewModel();

            _mockEiaService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockSnapshot);
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockHistorical);
            _mockDashboardService.Setup(s => s.BuildDashboard(mockSnapshot, mockHistorical)).Returns(expectedViewModel);

            // Act
            await _controller.Index();

            // Assert
            _mockDashboardService.Verify(s => s.BuildDashboard(mockSnapshot, mockHistorical), Times.Once);
        }

        [Fact]
        public async Task Index_ReturnsViewModelFromDashboardService()
        {
            // Arrange
            var expectedViewModel = new DashboardViewModel
            {
                TotalGenerationMegawatts = 1500,
                HydroPercentage = 75,
                TopSource = "Hydro",
                LatestSnapshot = new List<PowerData>(),
                PowerSourceSummaries = new List<PowerSourceSummary>()
            };

            _mockEiaService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(new List<PowerData>());
            _mockEiaService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(new List<PowerData>());
            _mockDashboardService.Setup(s => s.BuildDashboard(It.IsAny<List<PowerData>>(), It.IsAny<List<PowerData>>())).Returns(expectedViewModel);

            // Act
            var result = await _controller.Index();
            var viewResult = (ViewResult)result;
            var model = viewResult.Model as DashboardViewModel;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(1500, model.TotalGenerationMegawatts);
            Assert.Equal(75, model.HydroPercentage);
            Assert.Equal("Hydro", model.TopSource);
        }
    }
}