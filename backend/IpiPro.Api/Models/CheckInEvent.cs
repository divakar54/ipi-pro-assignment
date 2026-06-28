using System;

namespace IpiPro.Api.Models
{
    public class CheckInEvent
    {
        public int Id { get; set; }
        public int LabId { get; set; }
        public int ManifestId { get; set; }
        public int? SpecimenId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime At { get; set; }

        // Navigation properties
        public Lab? Lab { get; set; }
        public Manifest? Manifest { get; set; }
        public Specimen? Specimen { get; set; }
    }
}
