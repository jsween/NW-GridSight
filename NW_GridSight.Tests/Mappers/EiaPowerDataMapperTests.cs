using NW_GridSight.Mappers;
using System.Text.Json;

namespace NW_GridSight.Tests.Mappers
{
    public class EiaPowerDataMapperTests
    {
        [Fact]
        public void MapResponse_ReturnsPowerData_WhenJsonIsValid()
        {
            // Arrange
            string json = @"{
                ""response"": {
                    ""data"": [
                        {
                            ""period"": ""2026-04-28T06"",
                            ""respondent-name"": ""Bonneville Power Administration"",
                            ""type-name"": ""Hydro"",
                            ""value"": ""1002""
                        }
                    ]
                }
            }";
            // Act
            var result = EiaPowerDataMapper.MapResponse(json);
            // Assert
            Assert.Single(result);
            var powerData = result[0];
            Assert.Equal("Bonneville Power Administration", result[0].Region);
            Assert.Equal("Hydro", result[0].Source);
            Assert.Equal(1002, result[0].GenerationMegawatts);
            Assert.Equal(new DateTime(2026, 4, 28, 6, 0, 0, DateTimeKind.Utc),
                            powerData.TimestampUtc);
            Assert.Equal(DateTimeKind.Utc, powerData.TimestampUtc.Kind);
        }

        [Fact]
        public void MapResponse_ReturnsEmptyList_WhenDataIsMissing()
        {
            var json = """
            {
              "response": {}
            }
            """;

            var results = EiaPowerDataMapper.MapResponse(json);

            Assert.Empty(results);
        }

        [Fact]
        public void MapResponse_ReturnsEmptyList_WhenResponseIsMissing()
        {
            var json = "{}";

            var results = EiaPowerDataMapper.MapResponse(json);

            Assert.Empty(results);
        }

        [Fact]
        public void MapResponse_HandlesMultipleRecords()
        {
            var json = """
            {
              "response": {
                "data": [
                  {
                    "period": "2026-04-28T06",
                    "respondent-name": "Avista Corporation",
                    "type-name": "Natural Gas",
                    "value": "500"
                  },
                  {
                    "period": "2026-04-28T07",
                    "respondent-name": "Portland General Electric Company",
                    "type-name": "Solar",
                    "value": "250"
                  }
                ]
              }
            }
            """;

            var results = EiaPowerDataMapper.MapResponse(json);

            Assert.Equal(2, results.Count);
            Assert.Equal("Avista Corporation", results[0].Region);
            Assert.Equal("Natural Gas", results[0].Source);
            Assert.Equal("Portland General Electric Company", results[1].Region);
            Assert.Equal("Solar", results[1].Source);
            Assert.Equal(250, results[1].GenerationMegawatts);
        }

        [Theory]
        [InlineData("hydro", "Hydro")]
        [InlineData("natural gas", "Natural Gas")]
        [InlineData("coal", "Coal")]
        [InlineData("nuclear", "Nuclear")]
        [InlineData("wind", "Wind")]
        [InlineData("batter", "Battery Storage")]
        public void MapToPowerData_NormalizesSourceNames(string input, string expected)
        {
            var json = $$"""
            {
              "period": "2026-04-28T06",
              "respondent-name": "Test Region",
              "type-name": "{{input}}",
              "value": "100"
            }
            """;

            using var doc = JsonDocument.Parse(json);
            var result = EiaPowerDataMapper.MapToPowerData(doc.RootElement);

            Assert.Equal(expected, result.Source);
        }

        [Fact]
        public void MapToPowerData_HandlesInvalidValue_ReturnsZero()
        {
            var json = """
            {
              "period": "2026-04-28T06",
              "respondent-name": "Test Region",
              "type-name": "Hydro",
              "value": "invalid"
            }
            """;

            using var doc = JsonDocument.Parse(json);
            var result = EiaPowerDataMapper.MapToPowerData(doc.RootElement);

            Assert.Equal(0, result.GenerationMegawatts);
        }

        [Fact]
        public void MapToPowerData_HandlesMissingProperties()
        {
            var json = "{}";

            using var doc = JsonDocument.Parse(json);
            var result = EiaPowerDataMapper.MapToPowerData(doc.RootElement);

            Assert.Equal("Unknown", result.Region);
            Assert.Equal("Unknown", result.Source);
            Assert.Equal(0, result.GenerationMegawatts);
        }

        [Fact]
        public void MapToPowerData_HandlesInvalidDateFormat()
        {
            var json = """
            {
              "period": "invalid-date",
              "respondent-name": "Test Region",
              "type-name": "Hydro",
              "value": "100"
            }
            """;

            using var doc = JsonDocument.Parse(json);
            var result = EiaPowerDataMapper.MapToPowerData(doc.RootElement);

            // Should default to UtcNow when parsing fails
            Assert.Equal(DateTimeKind.Utc, result.TimestampUtc.Kind);
        }
    }
}
