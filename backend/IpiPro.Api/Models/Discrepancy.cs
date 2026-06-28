namespace IpiPro.Api.Models
{
    public enum DiscrepancyType
    {
        Missing = 0,
        OffManifest = 1
    }

    public enum DiscrepancyStatus
    {
        Open = 0,
        Resolved = 1
    }

    public class Discrepancy
    {
        public int Id { get; set; }
        public int LabId { get; set; }
        public int ManifestId { get; set; }
        public int SpecimenId { get; set; }
        public DiscrepancyType Type { get; set; }
        public string Note { get; set; } = string.Empty;
        public DiscrepancyStatus Status { get; set; }
        public string? ResolutionNote { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        public Lab? Lab { get; set; }
        public Manifest? Manifest { get; set; }
        public Specimen? Specimen { get; set; }
    }
}
