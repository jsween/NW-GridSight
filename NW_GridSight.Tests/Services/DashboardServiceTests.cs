using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NW_GridSight.Models;
using NW_GridSight.Services;

namespace NW_GridSight.Tests.Services
{
    public class DashboardServiceTests
    {
        private readonly Mock<IClock> _mockClock;
        private readonly Mock<IEiaService> _mockEiaService;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<ILogger<DashboardService>> _mockLogger;
        private readonly DashboardService _service;

        public DashboardServiceTests()
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

        #region Percentage Calculation Tests

        [Fact]
        public void BuildDashboard_CalculatesPercentagesCorrectly()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 600, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Solar", GenerationMegawatts = 100, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            var hydroSummary = result.PowerSourceSummaries.First(s => s.PowerSource == "Hydro");
            var windSummary = result.PowerSourceSummaries.First(s => s.PowerSource == "Wind");
            var solarSummary = result.PowerSourceSummaries.First(s => s.PowerSource == "Solar");

            Assert.Equal(60, hydroSummary.Percentage); // 600/1000 = 60%
            Assert.Equal(30, windSummary.Percentage);  // 300/1000 = 30%
            Assert.Equal(10, solarSummary.Percentage); // 100/1000 = 10%
        }

        [Fact]
        public void BuildDashboard_PercentagesAddUpTo100()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Solar", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            var totalPercentage = result.PowerSourceSummaries.Sum(s => s.Percentage);
            Assert.InRange(totalPercentage, 99, 100); // Allow for rounding
        }

        [Fact]
        public void BuildDashboard_Percentage_IsZero_WhenNoData()
        {
            // Arrange
            var emptySnapshot = new List<PowerData>();

            // Act
            var result = _service.BuildDashboard(emptySnapshot, []);

            // Assert
            Assert.Empty(result.PowerSourceSummaries);
        }

        [Fact]
        public void BuildDashboard_Percentage_HandlesZeroGeneration()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 0, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            var hydroSummary = result.PowerSourceSummaries.First(s => s.PowerSource == "Hydro");
            Assert.Equal(0, hydroSummary.Percentage);
        }

        [Fact]
        public void BuildDashboard_Percentage_RoundsDown()
        {
            // Arrange - 501/1000 = 50.1% should round down to 50
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 501, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 499, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            var hydroSummary = result.PowerSourceSummaries.First(s => s.PowerSource == "Hydro");
            Assert.Equal(50, hydroSummary.Percentage);
        }

        [Fact]
        public void BuildDashboard_Percentage_Is100_WhenOnlyOneSource()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 1000, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            var hydroSummary = result.PowerSourceSummaries.First(s => s.PowerSource == "Hydro");
            Assert.Equal(100, hydroSummary.Percentage);
        }

        #endregion

        #region PNW Region Filtering Tests

        [Fact]
        public void BuildDashboard_FiltersToPnwRegionsInSnapshot()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "California ISO", Source = "Solar", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.Equal(2, result.LatestSnapshot.Count);
            Assert.DoesNotContain(result.LatestSnapshot, x => x.Region == "California ISO");
        }

        [Fact]
        public void BuildDashboard_IncludesAllThreePnwRegions()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 100, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Wind", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Solar", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.Equal(3, result.LatestSnapshot.Count);
            Assert.Contains(result.LatestSnapshot, x => x.Region == "Avista Corporation");
            Assert.Contains(result.LatestSnapshot, x => x.Region == "PacifiCorp West");
            Assert.Contains(result.LatestSnapshot, x => x.Region == "Portland General Electric Company");
        }

        #endregion

        #region Power Source Summaries Tests

        [Fact]
        public void BuildDashboard_GroupsByPowerSource()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "PacifiCorp West", Source = "Hydro", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Wind", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.Equal(2, result.PowerSourceSummaries.Count);
            var hydroSummary = result.PowerSourceSummaries.First(x => x.PowerSource == "Hydro");
            Assert.Equal(800, hydroSummary.GenerationMegawatts);
        }

        [Fact]
        public void BuildDashboard_SummariesAreOrderedByGenerationDescending()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Wind", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 800, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Avista Corporation", Source = "Solar", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.Equal(3, result.PowerSourceSummaries.Count);
            Assert.Equal("Hydro", result.PowerSourceSummaries[0].PowerSource); // 800 MW
            Assert.Equal("Solar", result.PowerSourceSummaries[1].PowerSource); // 500 MW
            Assert.Equal("Wind", result.PowerSourceSummaries[2].PowerSource);  // 200 MW
        }

        #endregion

        #region Total Generation Tests

        [Fact]
        public void BuildDashboard_CalculatesTotalGenerationCorrectly()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Avista Corporation", Source = "Wind", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.Equal(800, result.TotalGenerationMegawatts);
        }

        [Fact]
        public void BuildDashboard_TotalGeneration_IsZero_WhenNoData()
        {
            // Arrange
            var emptySnapshot = new List<PowerData>();

            // Act
            var result = _service.BuildDashboard(emptySnapshot, []);

            // Assert
            Assert.Equal(0, result.TotalGenerationMegawatts);
        }

        #endregion

        #region Hydro Percentage Tests

        [Fact]
        public void BuildDashboard_CalculatesHydroPercentageCorrectly()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 600, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Avista Corporation", Source = "Wind", GenerationMegawatts = 400, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.Equal(60, result.HydroPercentage);
        }

        [Fact]
        public void BuildDashboard_HydroPercentage_IsZero_WhenNoHydro()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Wind", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Avista Corporation", Source = "Solar", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.Equal(0, result.HydroPercentage);
        }

        #endregion

        #region Top Source Tests

        [Fact]
        public void BuildDashboard_IdentifiesTopSource()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 800, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Avista Corporation", Source = "Wind", GenerationMegawatts = 200, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.Equal("Hydro", result.TopSource);
        }

        [Fact]
        public void BuildDashboard_TopSource_IsUnknown_WhenNoData()
        {
            // Arrange
            var emptySnapshot = new List<PowerData>();

            // Act
            var result = _service.BuildDashboard(emptySnapshot, []);

            // Assert
            Assert.Equal("Unknown", result.TopSource);
        }

        #endregion

        #region Historical Data Tests

        [Fact]
        public void BuildDashboard_PassesHistoricalDataToViewModel()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };
            var historicalData = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 450, TimestampUtc = DateTime.UtcNow.AddHours(-1) },
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 400, TimestampUtc = DateTime.UtcNow.AddHours(-2) }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, historicalData);

            // Assert
            Assert.NotNull(result.HistoricalData);
            Assert.Equal(2, result.HistoricalData.Count);
            Assert.Equal(historicalData, result.HistoricalData);
        }

        [Fact]
        public void BuildDashboard_HandlesEmptyHistoricalData()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.NotNull(result.HistoricalData);
            Assert.Empty(result.HistoricalData);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void BuildDashboard_HandlesNegativeGeneration_ForBatteryStorage()
        {
            // Arrange - Battery storage can be negative when charging
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 1000, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Avista Corporation", Source = "Battery Storage", GenerationMegawatts = -50, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert - Should handle negative values without crashing
            Assert.Equal(950, result.TotalGenerationMegawatts); // 1000 + (-50) = 950
            var batterySummary = result.PowerSourceSummaries.FirstOrDefault(s => s.PowerSource == "Battery Storage");
            Assert.NotNull(batterySummary);
            Assert.Equal(-50, batterySummary.GenerationMegawatts);
        }

        [Fact]
        public void BuildDashboard_SetsLastUpdatedUtc()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow }
            };
            var expectedTime = new DateTime(2026, 5, 5, 12, 30, 0, DateTimeKind.Utc);
            _mockClock.Setup(c => c.UtcNow).Returns(expectedTime);

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            Assert.Equal(expectedTime, result.LastUpdatedUtc);
            _mockClock.Verify(c => c.UtcNow, Times.Once);
        }

        #endregion

        #region Summaries Region Filtering Tests

        [Fact]
        public void BuildDashboard_Summaries_OnlyIncludePnwRegions()
        {
            // Arrange - Mix of PNW and non-PNW regions with same source
            var snapshot = new List<PowerData>
            {
                // PNW regions
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 500, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Portland General Electric Company", Source = "Hydro", GenerationMegawatts = 300, TimestampUtc = DateTime.UtcNow },
                // Non-PNW region - should be excluded
                new() { Region = "California ISO", Source = "Hydro", GenerationMegawatts = 1000, TimestampUtc = DateTime.UtcNow },
                new() { Region = "Texas Grid", Source = "Wind", GenerationMegawatts = 2000, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert
            var hydroSummary = result.PowerSourceSummaries.FirstOrDefault(s => s.PowerSource == "Hydro");

            // Hydro should only be 800 MW (500 + 300), NOT 1800 (which would include California)
            Assert.NotNull(hydroSummary);
            Assert.Equal(800, hydroSummary.GenerationMegawatts);

            // Wind should not appear at all (only non-PNW region has wind)
            var windSummary = result.PowerSourceSummaries.FirstOrDefault(s => s.PowerSource == "Wind");
            Assert.Null(windSummary);

            // Total generation should only be from PNW regions
            Assert.Equal(800, result.TotalGenerationMegawatts);

            // Should only have 1 summary (Hydro), not 2
            Assert.Single(result.PowerSourceSummaries);
        }

        [Fact]
        public void BuildDashboard_TotalGeneration_ExcludesNonPnwRegions()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 100, TimestampUtc = DateTime.UtcNow },
                new() { Region = "California ISO", Source = "Solar", GenerationMegawatts = 5000, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert - Should only count the 100 MW from Avista, not the 5000 from California
            Assert.Equal(100, result.TotalGenerationMegawatts);
            Assert.Single(result.PowerSourceSummaries);
            Assert.Equal("Hydro", result.PowerSourceSummaries[0].PowerSource);
        }

        [Fact]
        public void BuildDashboard_Percentages_CalculatedFromPnwRegionsOnly()
        {
            // Arrange
            var snapshot = new List<PowerData>
            {
                // PNW
                new() { Region = "Avista Corporation", Source = "Hydro", GenerationMegawatts = 100, TimestampUtc = DateTime.UtcNow },
                // Non-PNW - large amount that would skew percentages if included
                new() { Region = "California ISO", Source = "Solar", GenerationMegawatts = 9900, TimestampUtc = DateTime.UtcNow }
            };

            // Act
            var result = _service.BuildDashboard(snapshot, []);

            // Assert - Hydro should be 100%, not 1% (which it would be if California was included)
            var hydroSummary = result.PowerSourceSummaries.First(s => s.PowerSource == "Hydro");
            Assert.Equal(100, hydroSummary.Percentage);
        }

        #endregion
    }
}