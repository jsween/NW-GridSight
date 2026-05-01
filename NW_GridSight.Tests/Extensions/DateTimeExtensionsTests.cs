using NW_GridSight.Extensions;
using System.Globalization;

namespace NW_GridSight.Tests.Extensions
{
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void ToEiaString_FormatsUtcDateTimeCorrectly()
        {
            // Arrange
            var dateTime = new DateTime(2026, 4, 28, 14, 30, 45, DateTimeKind.Utc);

            // Act
            var result = dateTime.ToEiaString();

            // Assert
            Assert.Equal("2026-04-28T14", result);
        }

        [Fact]
        public void ToEiaString_ConvertsLocalTimeToUtc()
        {
            // Arrange
            var localTime = new DateTime(2026, 4, 28, 14, 30, 45, DateTimeKind.Local);
            var expectedUtcHour = localTime.ToUniversalTime().Hour;
            var expected = $"2026-04-28T{expectedUtcHour:D2}";

            // Act
            var result = localTime.ToEiaString();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToEiaString_HandlesUnspecifiedKind()
        {
            // Arrange
            var dateTime = new DateTime(2026, 4, 28, 14, 0, 0, DateTimeKind.Unspecified);

            // Act
            var result = dateTime.ToEiaString();

            // Assert
            // Should convert unspecified to UTC (treats as local time)
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}$", result);
        }

        [Fact]
        public void ToEiaString_UsesInvariantCulture()
        {
            // Arrange
            var dateTime = new DateTime(2026, 4, 28, 14, 30, 45, DateTimeKind.Utc);
            var currentCulture = CultureInfo.CurrentCulture;

            try
            {
                // Change culture to something different
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

                // Act
                var result = dateTime.ToEiaString();

                // Assert
                // Should still use invariant format, not French format
                Assert.Equal("2026-04-28T14", result);
                Assert.DoesNotContain("/", result); // French date format would use /
            }
            finally
            {
                // Restore original culture
                CultureInfo.CurrentCulture = currentCulture;
            }
        }

        [Fact]
        public void ToEiaString_IncludesOnlyHourNotMinutesOrSeconds()
        {
            // Arrange
            var dateTime = new DateTime(2026, 4, 28, 14, 59, 59, DateTimeKind.Utc);

            // Act
            var result = dateTime.ToEiaString();

            // Assert
            Assert.Equal("2026-04-28T14", result);
            Assert.DoesNotContain("59", result); // Should not include minutes or seconds
        }

        [Theory]
        [InlineData(2026, 1, 1, 0, "2026-01-01T00")]   // Start of year, midnight
        [InlineData(2026, 12, 31, 23, "2026-12-31T23")] // End of year, last hour
        [InlineData(2026, 2, 28, 12, "2026-02-28T12")]  // Regular day
        [InlineData(2024, 2, 29, 15, "2024-02-29T15")]  // Leap year
        public void ToEiaString_FormatsVariousDatesCorrectly(int year, int month, int day, int hour, string expected)
        {
            // Arrange
            var dateTime = new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc);

            // Act
            var result = dateTime.ToEiaString();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToEiaString_PreservesLeadingZerosInMonthAndDay()
        {
            // Arrange
            var dateTime = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

            // Act
            var result = dateTime.ToEiaString();

            // Assert
            Assert.Equal("2026-01-05T09", result);
            Assert.Contains("01", result); // Month should have leading zero
            Assert.Contains("05", result); // Day should have leading zero
            Assert.Contains("09", result); // Hour should have leading zero
        }

        [Fact]
        public void ToEiaString_MatchesEiaApiFormat()
        {
            // Arrange
            var dateTime = new DateTime(2026, 4, 28, 6, 0, 0, DateTimeKind.Utc);

            // Act
            var result = dateTime.ToEiaString();

            // Assert - This is the exact format EIA API expects in "period" field
            Assert.Equal("2026-04-28T06", result);
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}$", result);
        }

        [Fact]
        public void ToEiaString_IsReversibleWithDateTime_TryParseExact()
        {
            // Arrange
            var originalDateTime = new DateTime(2026, 4, 28, 14, 0, 0, DateTimeKind.Utc);

            // Act
            var eiaString = originalDateTime.ToEiaString();
            var parsed = DateTime.TryParseExact(
                eiaString,
                "yyyy-MM-dd'T'HH",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDateTime);

            // Assert
            Assert.True(parsed);
            Assert.Equal(originalDateTime.Year, parsedDateTime.Year);
            Assert.Equal(originalDateTime.Month, parsedDateTime.Month);
            Assert.Equal(originalDateTime.Day, parsedDateTime.Day);
            Assert.Equal(originalDateTime.Hour, parsedDateTime.Hour);
        }

        [Fact]
        public void ToEiaString_WorksWithDateTimeNow()
        {
            // Arrange
            var now = DateTime.UtcNow;

            // Act
            var result = now.ToEiaString();

            // Assert
            Assert.NotNull(result);
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}$", result);
            Assert.Contains(now.Year.ToString(), result);
        }
    }
}