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

        #region Total Generation Tests

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
        public async Task Index_TotalGeneration_IsZero_WhenNoData()
        {
            // Arrange
            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(new List<PowerData>());
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(new List<PowerData>());

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.Equal(0, model?.TotalGenerationMegawatts);
        }

        [Fact]
        public async Task Index_TotalGeneration_HandlesDecimalValues()
        {
            // Arrange - GenerationMegawatts is double but casts to int
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500.7, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 299.9, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert - Should truncate decimals (500 + 299 = 799)
            Assert.Equal(799, model?.TotalGenerationMegawatts);
        }

        [Fact]
        public async Task Index_TotalGeneration_OnlyCountsPnwRegions()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Non-PNW Region", Source = "Solar", GenerationMegawatts = 1000, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert - Should only count Avista (500), not the non-PNW region
            Assert.Equal(500, model?.TotalGenerationMegawatts);
        }

        #endregion

        #region Hydro Percentage Tests

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
        public async Task Index_HydroPercentage_IsZero_WhenNoHydro()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Wind", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Solar", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.Equal(0, model?.HydroPercentage);
        }

        [Fact]
        public async Task Index_HydroPercentage_IsZero_WhenNoData()
        {
            // Arrange
            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(new List<PowerData>());
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(new List<PowerData>());

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert - Should not crash with divide by zero
            Assert.Equal(0, model?.HydroPercentage);
        }

        [Fact]
        public async Task Index_HydroPercentage_Is100_WhenOnlyHydro()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.Equal(100, model?.HydroPercentage);
        }

        [Fact]
        public async Task Index_HydroPercentage_IsCaseInsensitive()
        {
            // Arrange - Test with different casings
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "HYDRO", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.Equal(100, model?.HydroPercentage);
        }

        [Fact]
        public async Task Index_HydroPercentage_RoundsDown()
        {
            // Arrange - 501 / 1000 = 50.1% should round down to 50
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 501, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 499, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.Equal(50, model?.HydroPercentage);
        }

        #endregion

        #region Top Source Tests

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
        public async Task Index_TopSource_IsUnknown_WhenNoData()
        {
            // Arrange
            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(new List<PowerData>());
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(new List<PowerData>());

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.Equal("Unknown", model?.TopSource);
        }

        [Fact]
        public async Task Index_TopSource_AggregatesMultipleRegions()
        {
            // Arrange - Hydro total: 800, Wind total: 900
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Hydro", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 900, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert - Wind (900) should be top, not Hydro (800)
            Assert.Equal("Wind", model?.TopSource);
        }

        [Fact]
        public async Task Index_TopSource_HandlesTie()
        {
            // Arrange - Both sources have same generation
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert - Should return one of them (first by OrderByDescending)
            Assert.NotNull(model?.TopSource);
            Assert.NotEqual("Unknown", model?.TopSource);
            Assert.True(model?.TopSource == "Hydro" || model?.TopSource == "Wind");
        }

        #endregion

        #region Empty Data Tests

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

        [Fact]
        public async Task Index_EmptyData_DoesNotCrash()
        {
            // Arrange
            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(new List<PowerData>());
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(new List<PowerData>());

            // Act
            var result = await _controller.Index();

            // Assert - Just verify it doesn't throw
            Assert.NotNull(result);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Index_EmptyData_HasEmptySummaries()
        {
            // Arrange
            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(new List<PowerData>());
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(new List<PowerData>());

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.NotNull(model);
            Assert.Empty(model.PowerSourceSummaries);
        }

        [Fact]
        public async Task Index_EmptyData_AllMetricsAreZeroOrDefault()
        {
            // Arrange
            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(new List<PowerData>());
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(new List<PowerData>());

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(0, model.TotalGenerationMegawatts);
            Assert.Equal(0, model.HydroPercentage);
            Assert.Equal("Unknown", model.TopSource);
            Assert.Empty(model.LatestSnapshot);
            Assert.Empty(model.PowerSourceSummaries);
        }

        #endregion

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
        public async Task Index_SummariesAreOrderedByGeneration()
        {
            // Arrange
            var mockData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Wind", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Hydro", GenerationMegawatts = 800, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Solar", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };

            _mockService.Setup(s => s.GetPowerDataSnapshot()).ReturnsAsync(mockData);
            _mockService.Setup(s => s.GetLast24HoursDataAsync()).ReturnsAsync(mockData);

            // Act
            var result = await _controller.Index();
            var model = ((ViewResult)result).Model as DashboardViewModel;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(3, model.PowerSourceSummaries.Count);
            Assert.Equal("Hydro", model.PowerSourceSummaries[0].PowerSource); // 800 MW
            Assert.Equal("Solar", model.PowerSourceSummaries[1].PowerSource); // 500 MW
            Assert.Equal("Wind", model.PowerSourceSummaries[2].PowerSource);  // 200 MW
        }
    }
}