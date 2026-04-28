namespace NW_GridSight.Models
{
    public class PowerData
    {
        public string Region { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public double GenerationMegawatts { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
