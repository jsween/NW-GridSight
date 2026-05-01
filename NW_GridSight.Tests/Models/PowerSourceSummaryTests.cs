using NW_GridSight.Models;

namespace NW_GridSight.Tests.Models
{
    public class PowerSourceSummaryTests
    {
        [Fact]
        public void PowerSourceSummary_InitializesWithDefaults()
        {
            // Act
            var summary = new PowerSourceSummary();

            // Assert
            Assert.Equal(string.Empty, summary.PowerSource);
            Assert.Equal(0, summary.GenerationMegawatts);
        }

        [Fact]
        public void PowerSourceSummary_CanSetAllProperties()
        {
            // Act
            var summary = new PowerSourceSummary
            {
                PowerSource = "Hydro",
                GenerationMegawatts = 5000
            };

            // Assert
            Assert.Equal("Hydro", summary.PowerSource);
            Assert.Equal(5000, summary.GenerationMegawatts);
        }

        [Theory]
        [InlineData("Hydro", 5000)]
        [InlineData("Wind", 3000)]
        [InlineData("Solar", 2000)]
        [InlineData("Natural Gas", 1500)]
        [InlineData("Nuclear", 4000)]
        [InlineData("Coal", 1000)]
        public void PowerSourceSummary_AcceptsVariousPowerSources(string source, int generation)
        {
            // Act
            var summary = new PowerSourceSummary
            {
                PowerSource = source,
                GenerationMegawatts = generation
            };

            // Assert
            Assert.Equal(source, summary.PowerSource);
            Assert.Equal(generation, summary.GenerationMegawatts);
        }

        [Fact]
        public void PowerSourceSummary_GenerationMegawatts_CanBeZero()
        {
            // Act
            var summary = new PowerSourceSummary
            {
                PowerSource = "Solar",
                GenerationMegawatts = 0
            };

            // Assert
            Assert.Equal(0, summary.GenerationMegawatts);
        }

        [Fact]
        public void PowerSourceSummary_SupportsObjectInitializerSyntax()
        {
            // Act
            var summary = new PowerSourceSummary
            {
                PowerSource = "Test",
                GenerationMegawatts = 100
            };

            // Assert
            Assert.NotNull(summary);
            Assert.Equal("Test", summary.PowerSource);
        }

        [Fact]
        public void PowerSourceSummary_CanBeUsedInList()
        {
            // Act
            var summaries = new List<PowerSourceSummary>
            {
                new() { PowerSource = "Hydro", GenerationMegawatts = 5000 },
                new() { PowerSource = "Wind", GenerationMegawatts = 3000 }
            };

            // Assert
            Assert.Equal(2, summaries.Count);
            Assert.Equal("Hydro", summaries[0].PowerSource);
            Assert.Equal("Wind", summaries[1].PowerSource);
        }

        [Fact]
        public void PowerSourceSummary_CanBeSortedByGeneration()
        {
            // Arrange
            var summaries = new List<PowerSourceSummary>
            {
                new() { PowerSource = "Wind", GenerationMegawatts = 3000 },
                new() { PowerSource = "Hydro", GenerationMegawatts = 5000 },
                new() { PowerSource = "Solar", GenerationMegawatts = 2000 }
            };

            // Act
            var sorted = summaries.OrderByDescending(s => s.GenerationMegawatts).ToList();

            // Assert
            Assert.Equal("Hydro", sorted[0].PowerSource);
            Assert.Equal("Wind", sorted[1].PowerSource);
            Assert.Equal("Solar", sorted[2].PowerSource);
        }
    }
}