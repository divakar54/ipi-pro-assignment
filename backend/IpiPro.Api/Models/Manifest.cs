using System;
using System.Collections.Generic;

namespace IpiPro.Api.Models
{
    public enum ManifestStatus
    {
        InTransit = 0,
        Open = 1,
        Closed = 2,
        ClosedWithDiscrepancy = 3
    }

    public class Manifest
    {
        public int Id { get; set; }
        public int LabId { get; set; }
        public int ClinicId { get; set; }
        public string Code { get; set; } = string.Empty;
        public ManifestStatus Status { get; set; }
        public DateTime SentAt { get; set; }

        // Navigation properties
        public Lab? Lab { get; set; }
        public Clinic? Clinic { get; set; }
        public ICollection<Specimen> Specimens { get; set; } = new List<Specimen>();
        public ICollection<Discrepancy> Discrepancies { get; set; } = new List<Discrepancy>();
        public ICollection<CheckInEvent> CheckInEvents { get; set; } = new List<CheckInEvent>();
    }
}
