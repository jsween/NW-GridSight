namespace NW_GridSight.Models
{
    public class PowerData
    {
        public string Region { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public double GenerationMegaWatts { get; set; }
        public DateTime TimeStampUtc { get; set; }
    }
}
