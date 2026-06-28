using System.ComponentModel.DataAnnotations;

namespace IpiPro.Api.DTOs
{
    public class SpecimenInputDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        
        public string Patient { get; set; } = string.Empty;
        
        public string Site { get; set; } = string.Empty;
        
        public string Provider { get; set; } = string.Empty;
    }
}
