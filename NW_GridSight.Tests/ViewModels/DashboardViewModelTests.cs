using NW_GridSight.Models;
using NW_GridSight.ViewModels;

namespace NW_GridSight.Tests.ViewModels
{
    public class DashboardViewModelTests
    {
        [Fact]
        public void DashboardViewModel_InitializesWithDefaults()
        {
            // Act
            var viewModel = new DashboardViewModel();

            // Assert
            Assert.NotNull(viewModel.LatestSnapshot);
            Assert.Empty(viewModel.LatestSnapshot);
            Assert.NotNull(viewModel.HistoricalData);
            Assert.Empty(viewModel.HistoricalData);
            Assert.NotNull(viewModel.PowerSourceSummaries);
            Assert.Empty(viewModel.PowerSourceSummaries);
            Assert.Equal(0, viewModel.TotalGenerationMegawatts);
            Assert.Equal(0, viewModel.HydroPercentage);
            Assert.Null(viewModel.TopSource);
        }

        [Fact]
        public void DashboardViewModel_CanSetLatestSnapshot()
        {
            // Arrange
            var powerData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500 }
            };

            // Act
            var viewModel = new DashboardViewModel
            {
                LatestSnapshot = powerData
            };

            // Assert
            Assert.Single(viewModel.LatestSnapshot);
            Assert.Equal("Avista Corporation", viewModel.LatestSnapshot[0].Region);
        }

        [Fact]
        public void DashboardViewModel_CanSetHistoricalData()
        {
            // Arrange
            var historicalData = new List<PowerData>
            {
                new() { Region = "Test", Source = "Hydro", GenerationMegawatts = 100, TimestampUtc = DateTime.UtcNow.AddHours(-1) },
                new() { Region = "Test", Source = "Hydro", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var viewModel = new DashboardViewModel
            {
                HistoricalData = historicalData
            };

            // Assert
            Assert.Equal(2, viewModel.HistoricalData.Count);
        }

        [Fact]
        public void DashboardViewModel_CanSetPowerSourceSummaries()
        {
            // Arrange
            var summaries = new List<PowerSourceSummary>
            {
                new() { PowerSource = "Hydro", GenerationMegawatts = 5000 },
                new() { PowerSource = "Wind", GenerationMegawatts = 3000 }
            };

            // Act
            var viewModel = new DashboardViewModel
            {
                PowerSourceSummaries = summaries
            };

            // Assert
            Assert.Equal(2, viewModel.PowerSourceSummaries.Count);
        }

        [Fact]
        public void DashboardViewModel_CanSetTotalGeneration()
        {
            // Act
            var viewModel = new DashboardViewModel
            {
                TotalGenerationMegawatts = 8000
            };

            // Assert
            Assert.Equal(8000, viewModel.TotalGenerationMegawatts);
        }

        [Fact]
        public void DashboardViewModel_CanSetHydroPercentage()
        {
            // Act
            var viewModel = new DashboardViewModel
            {
                HydroPercentage = 65
            };

            // Assert
            Assert.Equal(65, viewModel.HydroPercentage);
        }

        [Fact]
        public void DashboardViewModel_CanSetTopSource()
        {
            // Act
            var viewModel = new DashboardViewModel
            {
                TopSource = "Hydro"
            };

            // Assert
            Assert.Equal("Hydro", viewModel.TopSource);
        }

        [Fact]
        public void DashboardViewModel_SupportsFullyPopulatedViewModel()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;

            // Act
            var viewModel = new DashboardViewModel
            {
                LatestSnapshot = new List<PowerData>
                {
                    new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = timestamp }
                },
                HistoricalData = new List<PowerData>
                {
                    new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 450, TimestampUtc = timestamp.AddHours(-1) }
                },
                PowerSourceSummaries = new List<PowerSourceSummary>
                {
                    new() { PowerSource = "Hydro", GenerationMegawatts = 5000 }
                },
                TotalGenerationMegawatts = 8000,
                HydroPercentage = 62,
                TopSource = "Hydro",
                LastUpdatedUtc = timestamp
            };

            // Assert
            Assert.NotNull(viewModel);
            Assert.Single(viewModel.LatestSnapshot);
            Assert.Single(viewModel.HistoricalData);
            Assert.Single(viewModel.PowerSourceSummaries);
            Assert.Equal(8000, viewModel.TotalGenerationMegawatts);
            Assert.Equal(62, viewModel.HydroPercentage);
            Assert.Equal("Hydro", viewModel.TopSource);
            Assert.Equal(timestamp, viewModel.LastUpdatedUtc);
        }

        [Fact]
        public void DashboardViewModel_HydroPercentage_AcceptsZeroToHundred()
        {
            // Arrange & Act
            var viewModel = new DashboardViewModel
            {
                HydroPercentage = 0
            };

            // Assert
            Assert.Equal(0, viewModel.HydroPercentage);

            // Act
            viewModel.HydroPercentage = 100;

            // Assert
            Assert.Equal(100, viewModel.HydroPercentage);
        }

        [Fact]
        public void DashboardViewModel_EmptyLatestSnapshot_IsValid()
        {
            // Act
            var viewModel = new DashboardViewModel
            {
                LatestSnapshot = new List<PowerData>(),
                TotalGenerationMegawatts = 0
            };

            // Assert
            Assert.Empty(viewModel.LatestSnapshot);
            Assert.Equal(0, viewModel.TotalGenerationMegawatts);
        }

        [Fact]
        public void DashboardViewModel_CanGroupHistoricalDataBySource()
        {
            // Arrange
            var viewModel = new DashboardViewModel
            {
                HistoricalData = new List<PowerData>
                {
                    new() { Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow.AddHours(-2) },
                    new() { Source = "Hydro", GenerationMegawatts = 550, TimestampUtc = DateTime.UtcNow.AddHours(-1) },
                    new() { Source = "Wind", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow.AddHours(-1) }
                }
            };

            // Act
            var groupedBySource = viewModel.HistoricalData
                .GroupBy(x => x.Source)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Assert
            Assert.Equal(2, groupedBySource.Count);
            Assert.Equal(2, groupedBySource["Hydro"].Count);
            Assert.Single(groupedBySource["Wind"]);
        }

        [Fact]
        public void DashboardViewModel_PowerSourceSummaries_CanBeOrderedByGeneration()
        {
            // Arrange
            var viewModel = new DashboardViewModel
            {
                PowerSourceSummaries = new List<PowerSourceSummary>
                {
                    new() { PowerSource = "Wind", GenerationMegawatts = 3000 },
                    new() { PowerSource = "Hydro", GenerationMegawatts = 5000 },
                    new() { PowerSource = "Solar", GenerationMegawatts = 2000 }
                }
            };

            // Act
            var ordered = viewModel.PowerSourceSummaries
                .OrderByDescending(s => s.GenerationMegawatts)
                .ToList();

            // Assert
            Assert.Equal("Hydro", ordered[0].PowerSource);
            Assert.Equal("Wind", ordered[1].PowerSource);
            Assert.Equal("Solar", ordered[2].PowerSource);
        }
    }
}