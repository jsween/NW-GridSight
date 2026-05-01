using NW_GridSight.Models;

namespace NW_GridSight.Tests.Models
{
    public class PowerDataTests
    {
        [Fact]
        public void PowerData_InitializesWithDefaults()
        {
            // Act
            var powerData = new PowerData();

            // Assert
            Assert.Equal(string.Empty, powerData.Region);
            Assert.Equal(string.Empty, powerData.Source);
            Assert.Equal(0, powerData.GenerationMegawatts);
            Assert.Equal(default, powerData.TimestampUtc);
        }

        [Fact]
        public void PowerData_CanSetAllProperties()
        {
            // Arrange
            var timestamp = new DateTime(2026, 4, 28, 14, 0, 0, DateTimeKind.Utc);

            // Act
            var powerData = new PowerData
            {
                Region = "Avista Corporation",
                Source = "Hydro",
                GenerationMegawatts = 1500.5,
                TimestampUtc = timestamp
            };

            // Assert
            Assert.Equal("Avista Corporation", powerData.Region);
            Assert.Equal("Hydro", powerData.Source);
            Assert.Equal(1500.5, powerData.GenerationMegawatts);
            Assert.Equal(timestamp, powerData.TimestampUtc);
        }

        [Fact]
        public void PowerData_AcceptsNegativeGeneration()
        {
            // Arrange & Act
            var powerData = new PowerData
            {
                GenerationMegawatts = -100
            };

            // Assert
            Assert.Equal(-100, powerData.GenerationMegawatts);
        }

        [Fact]
        public void PowerData_AcceptsZeroGeneration()
        {
            // Arrange & Act
            var powerData = new PowerData
            {
                GenerationMegawatts = 0
            };

            // Assert
            Assert.Equal(0, powerData.GenerationMegawatts);
        }

        [Fact]
        public void PowerData_TimestampUtc_PreservesUtcKind()
        {
            // Arrange
            var utcTime = new DateTime(2026, 4, 28, 14, 0, 0, DateTimeKind.Utc);

            // Act
            var powerData = new PowerData
            {
                TimestampUtc = utcTime
            };

            // Assert
            Assert.Equal(DateTimeKind.Utc, powerData.TimestampUtc.Kind);
        }

        [Theory]
        [InlineData("Avista Corporation", "Hydro")]
        [InlineData("Portland General Electric Company", "Wind")]
        [InlineData("PacifiCorp West", "Solar")]
        [InlineData("Bonneville Power Administration", "Natural Gas")]
        public void PowerData_AcceptsPnwRegionsAndSources(string region, string source)
        {
            // Act
            var powerData = new PowerData
            {
                Region = region,
                Source = source,
                GenerationMegawatts = 500
            };

            // Assert
            Assert.Equal(region, powerData.Region);
            Assert.Equal(source, powerData.Source);
        }

        [Fact]
        public void PowerData_SupportsObjectInitializerSyntax()
        {
            // Act
            var powerData = new PowerData
            {
                Region = "Test Region",
                Source = "Test Source",
                GenerationMegawatts = 100,
                TimestampUtc = DateTime.UtcNow
            };

            // Assert
            Assert.NotNull(powerData);
            Assert.Equal("Test Region", powerData.Region);
        }

        [Fact]
        public void PowerData_GenerationMegawatts_HandlesDecimalPrecision()
        {
            // Arrange & Act
            var powerData = new PowerData
            {
                GenerationMegawatts = 1234.56789
            };

            // Assert
            Assert.Equal(1234.56789, powerData.GenerationMegawatts, precision: 5);
        }
    }
}