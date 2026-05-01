using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NW_GridSight.Configuration;
using NW_GridSight.Services;
using System.Net;

namespace NW_GridSight.Tests.Services
{
    public class EiaServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly IOptions<EiaApiOptions> _options;
        private readonly ILogger<EiaService> _logger;
        private readonly Mock<IClock> _clockMock;

        public EiaServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _options = Options.Create(new EiaApiOptions
            {
                BaseUrl = "https://api.eia.gov/v2",
                ApiKey = "test-api-key"
            });
            _logger = NullLogger<EiaService>.Instance;
            _clockMock = new Mock<IClock>();

            // Set a fixed time for predictable tests
            _clockMock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public async Task GetPowerDataSnapshot_ReturnsData_WhenApiReturnsValidJson()
        {
            // Arrange
            var jsonResponse = """
            {
              "response": {
                "data": [
                  {
                    "period": "2026-04-28T06",
                    "respondent-name": "Avista Corporation",
                    "type-name": "Hydro",
                    "value": "500"
                  }
                ]
              }
            }
            """;

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);

            // Act
            var result = await service.GetPowerDataSnapshot();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Avista Corporation", result[0].Region);
            Assert.Equal("Hydro", result[0].Source);
            Assert.Equal(500, result[0].GenerationMegawatts);
        }

        [Fact]
        public async Task GetPowerDataSnapshot_ThrowsException_WhenApiReturnsError()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized
                });

            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                () => service.GetPowerDataSnapshot());
        }

        [Fact]
        public async Task GetLast24HoursDataAsync_UsesClockForDateRange()
        {
            // Arrange
            var fixedTime = new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Utc);
            _clockMock.Setup(c => c.UtcNow).Returns(fixedTime);

            HttpRequestMessage? capturedRequest = null;

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("""{"response": {"data": []}}""")
                });

            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);

            // Act
            await service.GetLast24HoursDataAsync();

            // Assert
            Assert.NotNull(capturedRequest);
            // Verify the clock was used to calculate the date range
            _clockMock.Verify(c => c.UtcNow, Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetPowerDataSnapshot_ReturnsLatestDataPerSource()
        {
            // Arrange
            var jsonResponse = """
    {
      "response": {
        "data": [
          {
            "period": "2026-04-28T05",
            "respondent-name": "Avista Corporation",
            "type-name": "Hydro",
            "value": "400"
          },
          {
            "period": "2026-04-28T06",
            "respondent-name": "Avista Corporation",
            "type-name": "Hydro",
            "value": "500"
          },
          {
            "period": "2026-04-28T06",
            "respondent-name": "Portland General Electric Company",
            "type-name": "Wind",
            "value": "300"
          }
        ]
      }
    }
    """;

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);

            // Act
            var result = await service.GetPowerDataSnapshot();

            // Assert
            Assert.Equal(2, result.Count); // Should have 2 sources: Hydro and Wind

            var hydro = result.First(x => x.Source == "Hydro");
            Assert.Equal(500, hydro.GenerationMegawatts); // Should be the latest (06:00, not 05:00)
            Assert.Equal(new DateTime(2026, 4, 28, 6, 0, 0, DateTimeKind.Utc), hydro.TimestampUtc);
        }

        [Fact]
        public async Task GetLast24HoursDataAsync_ReturnsAllHistoricalData()
        {
            // Arrange
            var jsonResponse = """
    {
      "response": {
        "data": [
          {
            "period": "2026-04-27T12",
            "respondent-name": "Avista Corporation",
            "type-name": "Hydro",
            "value": "450"
          },
          {
            "period": "2026-04-28T06",
            "respondent-name": "Avista Corporation",
            "type-name": "Hydro",
            "value": "500"
          }
        ]
      }
    }
    """;

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);

            // Act
            var result = await service.GetLast24HoursDataAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.True(item.GenerationMegawatts > 0));
        }

        [Fact]
        public async Task GetLast24HoursDataAsync_BuildsCorrectUrl()
        {
            // Arrange
            var fixedTime = new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Utc);
            _clockMock.Setup(c => c.UtcNow).Returns(fixedTime);

            HttpRequestMessage? capturedRequest = null;

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("""{"response": {"data": []}}""")
                });

            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);

            // Act
            await service.GetLast24HoursDataAsync();

            // Assert
            Assert.NotNull(capturedRequest);
            var url = capturedRequest.RequestUri?.ToString();
            Assert.Contains("api.eia.gov/v2", url);
            Assert.Contains("test-api-key", url);
            Assert.Contains("frequency=hourly", url);
            Assert.Contains("facets[respondent][]=AVA", url);
            Assert.Contains("facets[respondent][]=BPA", url);
            Assert.Contains("facets[respondent][]=PGE", url);
            Assert.Contains("facets[respondent][]=PACW", url);
        }

        [Fact]
        public async Task GetPowerDataSnapshot_HandlesEmptyResponse()
        {
            // Arrange
            var jsonResponse = """{"response": {"data": []}}""";

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);

            // Act
            var result = await service.GetPowerDataSnapshot();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task GetLast24HoursDataAsync_ThrowsHttpRequestException_ForErrorStatusCodes(HttpStatusCode statusCode)
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent("Error response body")
                });

            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                () => service.GetLast24HoursDataAsync());

            Assert.Contains("EIA API request failed", exception.Message);
            Assert.Contains(statusCode.ToString(), exception.Message); ;
        }

        [Fact]
        public void BuildRequestUrl_GeneratesCorrectFormat()
        {
            // Arrange
            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);
            var start = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Utc);

            // Act
            var url = service.BuildRequestUrl(start, end);

            // Assert
            Assert.Contains("https://api.eia.gov/v2", url);
            Assert.Contains("electricity/rto/fuel-type-data/data", url);
            Assert.Contains("api_key=test-api-key", url);
            Assert.Contains("frequency=hourly", url);
            Assert.Contains("data[0]=value", url);
        }

        [Fact]
        public void BuildRequestUrl_IncludesAllPnwRespondents()
        {
            // Arrange
            var service = new EiaService(_httpClient, _options, _logger, _clockMock.Object);
            var start = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Utc);

            // Act
            var url = service.BuildRequestUrl(start, end);

            // Assert
            Assert.Contains("facets[respondent][]=AVA", url);  // Avista
            Assert.Contains("facets[respondent][]=BPA", url);  // Bonneville
            Assert.Contains("facets[respondent][]=PGE", url);  // Portland General Electric
            Assert.Contains("facets[respondent][]=PACW", url); // PacifiCorp West
        }
    }
}