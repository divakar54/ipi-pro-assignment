namespace IpiPro.Api.Models
{
    public enum SpecimenStatus
    {
        Pending = 0,
        Received = 1,
        Flagged = 2,
        Added = 3
    }

    public class Specimen
    {
        public int Id { get; set; }
        public int LabId { get; set; }
        public int ManifestId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Patient { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public SpecimenStatus Status { get; set; }

        // Navigation properties
        public Lab? Lab { get; set; }
        public Manifest? Manifest { get; set; }
    }
}
