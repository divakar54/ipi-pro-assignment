namespace IpiPro.Api.Models
{
    public class Clinic
    {
        public int Id { get; set; }
        public int LabId { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public Lab? Lab { get; set; }
        public ICollection<Manifest> Manifests { get; set; } = new List<Manifest>();
    }
}
