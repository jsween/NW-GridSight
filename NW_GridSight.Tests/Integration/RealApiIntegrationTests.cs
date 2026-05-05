using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NW_GridSight.Configuration;
using NW_GridSight.Services;

namespace NW_GridSight.Tests.Integration
{
    public class RealApiIntegrationTests
    {
        private readonly IConfiguration _configuration;

        public RealApiIntegrationTests()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(currentDirectory, "appsettings.Test.json");

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException(
                    $"appsettings.Test.json not found at: {configPath}");
            }

            _configuration = new ConfigurationBuilder()
                .SetBasePath(currentDirectory)
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false)
                .Build();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RequiresNetwork")]
        public async Task EiaService_CanFetchRealDataFromApi()
        {
            // Arrange
            var eiaOptions = _configuration.GetSection("EiaApi").Get<EiaApiOptions>();

            Assert.NotNull(eiaOptions);
            Assert.NotEmpty(eiaOptions.BaseUrl);
            Assert.NotEmpty(eiaOptions.ApiKey);

            var options = Options.Create(eiaOptions);
            var httpClient = new HttpClient();
            var logger = NullLogger<EiaService>.Instance;
            var clockMock = new Mock<IClock>();
            clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

            var service = new EiaService(httpClient, options, logger, clockMock.Object);

            // Act
            var result = await service.GetPowerDataSnapshot();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, item =>
            {
                Assert.NotEmpty(item.Region);
                Assert.NotEmpty(item.Source);
                // Battery storage can have negative values when charging
                if (item.Source.Equals("Battery Storage", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.True(item.GenerationMegawatts != 0); // Can be positive or negative, just not zero
                }
                else
                {
                    Assert.True(item.GenerationMegawatts >= 0);
                }
            });
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RequiresNetwork")]
        public async Task EiaService_GetLast24HoursData_ReturnsHistoricalData()
        {
            // Arrange
            var eiaOptions = _configuration.GetSection("EiaApi").Get<EiaApiOptions>();

            Assert.NotNull(eiaOptions);

            var options = Options.Create(eiaOptions);
            var httpClient = new HttpClient();
            var logger = NullLogger<EiaService>.Instance;
            var clockMock = new Mock<IClock>();
            clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

            var service = new EiaService(httpClient, options, logger, clockMock.Object);

            // Act
            var result = await service.GetLast24HoursDataAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);

            var timestamps = result.Select(x => x.TimestampUtc).Distinct().Count();
            Assert.True(timestamps > 1, "Should have multiple timestamps in 24-hour data");
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RequiresNetwork")]
        public async Task EiaService_ReturnsDataForPnwRegions()
        {
            // Arrange
            var eiaOptions = _configuration.GetSection("EiaApi").Get<EiaApiOptions>();
            var options = Options.Create(eiaOptions!);
            var httpClient = new HttpClient();
            var logger = NullLogger<EiaService>.Instance;
            var clockMock = new Mock<IClock>();
            clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

            var service = new EiaService(httpClient, options, logger, clockMock.Object);

            // Act
            var result = await service.GetLast24HoursDataAsync();

            // Assert
            Assert.NotEmpty(result);

            var regions = result.Select(x => x.Region).Distinct().ToList();

            // Should contain at least some PNW regions
            var pnwRegions = new[] { "Avista Corporation", "Bonneville Power Administration",
                                      "Portland General Electric Company", "PacifiCorp West" };

            Assert.True(regions.Any(r => pnwRegions.Contains(r)),
                "Result should contain at least one PNW region");
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RequiresNetwork")]
        public async Task EiaService_HandlesApiErrorGracefully()
        {
            // Arrange - Use invalid API key to force an error
            var invalidOptions = Options.Create(new EiaApiOptions
            {
                BaseUrl = "https://api.eia.gov/v2",
                ApiKey = "invalid-api-key-12345"
            });

            var httpClient = new HttpClient();
            var logger = NullLogger<EiaService>.Instance;
            var clockMock = new Mock<IClock>();
            clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

            var service = new EiaService(httpClient, invalidOptions, logger, clockMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => service.GetPowerDataSnapshot());
        }

        [Fact] // No traits - this is a fast unit test, not an integration test
        public void Configuration_LoadsEiaApiSettingsCorrectly()
        {
            // Act
            var eiaOptions = _configuration.GetSection("EiaApi").Get<EiaApiOptions>();

            // Assert
            Assert.NotNull(eiaOptions);
            Assert.Equal("https://api.eia.gov/v2/", eiaOptions.BaseUrl);
            Assert.NotEmpty(eiaOptions.ApiKey);
            Assert.True(eiaOptions.ApiKey.Length > 10, "API key should be a valid length");
        }
    }
}