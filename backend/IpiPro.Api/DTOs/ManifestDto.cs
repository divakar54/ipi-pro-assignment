using System;
using System.Collections.Generic;

namespace IpiPro.Api.DTOs
{
    public class ManifestDto
    {
        public int Id { get; set; }
        public int LabId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }

        public int PendingCount { get; set; }
        public int ReceivedCount { get; set; }
        public int FlaggedCount { get; set; }
        public int AddedCount { get; set; }

        public List<SpecimenDto> Specimens { get; set; } = new List<SpecimenDto>();
        public List<DiscrepancyDto> Discrepancies { get; set; } = new List<DiscrepancyDto>();
    }
}
