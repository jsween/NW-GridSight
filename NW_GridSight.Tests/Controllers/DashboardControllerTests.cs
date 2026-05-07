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
        private readonly Mock<IDashboardService> _mockDashboardService;
        private readonly DashboardController _controller;

        public DashboardControllerTests()
        {
            _mockDashboardService = new Mock<IDashboardService>();
            _controller = new DashboardController(_mockDashboardService.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewResult()
        {
            // Arrange
            var viewModel = CreateMockViewModel();
            _mockDashboardService.Setup(s => s.BuildDashboardAsync()).ReturnsAsync(viewModel);

            // Act
            var result = await _controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithDashboardViewModel()
        {
            // Arrange
            var viewModel = CreateMockViewModel();
            _mockDashboardService.Setup(s => s.BuildDashboardAsync()).ReturnsAsync(viewModel);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task Index_CallsDashboardServiceBuildDashboardAsync()
        {
            // Arrange
            var viewModel = CreateMockViewModel();
            _mockDashboardService.Setup(s => s.BuildDashboardAsync()).ReturnsAsync(viewModel);

            // Act
            await _controller.Index();

            // Assert
            _mockDashboardService.Verify(s => s.BuildDashboardAsync(), Times.Once);
        }

        [Fact]
        public async Task Index_ReturnsCorrectViewModel()
        {
            // Arrange
            var expectedViewModel = new DashboardViewModel
            {
                TotalGenerationMegawatts = 1500,
                HydroPercentage = 75,
                TopSource = "Hydro",
                LatestSnapshot = new List<PowerData>
                {
                    new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 1125, TimestampUtc = DateTime.UtcNow }
                },
                PowerSourceSummaries = new List<PowerSourceSummary>
                {
                    new() { PowerSource = "Hydro", GenerationMegawatts = 1125, Percentage = 75 }
                }
            };

            _mockDashboardService.Setup(s => s.BuildDashboardAsync()).ReturnsAsync(expectedViewModel);

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

        [Fact]
        public async Task Index_HandlesViewModelWithError()
        {
            // Arrange
            var errorViewModel = new DashboardViewModel
            {
                HasError = true,
                ErrorMessage = "API error",
                HistoricalData = new List<PowerData>(),
                PowerSourceSummaries = new List<PowerSourceSummary>()
            };

            _mockDashboardService.Setup(s => s.BuildDashboardAsync()).ReturnsAsync(errorViewModel);

            // Act
            var result = await _controller.Index();
            var viewResult = (ViewResult)result;
            var model = viewResult.Model as DashboardViewModel;

            // Assert
            Assert.NotNull(model);
            Assert.True(model.HasError);
            Assert.Equal("API error", model.ErrorMessage);
        }

        [Fact]
        public async Task Index_UsesDefaultView()
        {
            // Arrange
            var viewModel = CreateMockViewModel();
            _mockDashboardService.Setup(s => s.BuildDashboardAsync()).ReturnsAsync(viewModel);

            // Act
            var result = await _controller.Index();
            var viewResult = (ViewResult)result;

            // Assert
            Assert.Null(viewResult.ViewName); // Default view
        }

        private static DashboardViewModel CreateMockViewModel()
        {
            return new DashboardViewModel
            {
                TotalGenerationMegawatts = 800,
                HydroPercentage = 60,
                TopSource = "Hydro",
                LatestSnapshot = new List<PowerData>(),
                PowerSourceSummaries = new List<PowerSourceSummary>(),
                HistoricalData = new List<PowerData>()
            };
        }
    }
}