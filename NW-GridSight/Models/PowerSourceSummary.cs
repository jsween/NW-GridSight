namespace NW_GridSight.Models
{
    public class PowerSourceSummary
    {
        public string PowerSource { get; set; } = string.Empty;
        public int GenerationMegawatts { get; set; }
        public int Percentage { get; set; }
    }
}
