namespace IpiPro.Api.Models
{
    public class Lab
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<Clinic> Clinics { get; set; } = new List<Clinic>();
        public ICollection<Manifest> Manifests { get; set; } = new List<Manifest>();
    }
}
