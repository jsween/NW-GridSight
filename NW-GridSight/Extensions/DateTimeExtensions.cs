using System.Globalization;

namespace NW_GridSight.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToEiaString(this DateTime dateTime)
        {
            return dateTime
                .ToUniversalTime()
                .ToString("yyyy-MM-dd'T'HH", CultureInfo.InvariantCulture);
        }
    }
}
