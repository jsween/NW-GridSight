using NW_GridSight.Mappers;

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
    }
}
