namespace IpiPro.Api.DTOs
{
    public class SpecimenDto
    {
        public int Id { get; set; }
        public int ManifestId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Patient { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public DateTime? ReceivedAt { get; set; }
    }
}
