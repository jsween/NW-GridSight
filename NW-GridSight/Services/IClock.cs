namespace NW_GridSight.Services
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
