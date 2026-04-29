namespace NW_GridSight.Services
{
    public class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
