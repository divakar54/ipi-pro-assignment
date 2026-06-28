using System.ComponentModel.DataAnnotations;

namespace IpiPro.Api.DTOs
{
    public class ResolveDiscrepancyInput
    {
        [Required]
        public string Note { get; set; } = string.Empty;
    }
}
