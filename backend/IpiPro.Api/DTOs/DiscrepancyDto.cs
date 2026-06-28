namespace IpiPro.Api.DTOs
{
    public class DiscrepancyDto
    {
        public int Id { get; set; }
        public int ManifestId { get; set; }
        public int SpecimenId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
