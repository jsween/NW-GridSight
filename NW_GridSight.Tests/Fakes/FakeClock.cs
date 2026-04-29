using NW_GridSight.Services;

namespace NW_GridSight.Tests.Tests
{
    internal class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; }
    }
}
