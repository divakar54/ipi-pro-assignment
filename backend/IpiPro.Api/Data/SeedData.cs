using System;
using System.Collections.Generic;
using System.Linq;
using IpiPro.Api.Models;

namespace IpiPro.Api.Data
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext db)
        {
            if (db.Labs.Any())
            {
                return; // DB has been seeded
            }

            // Seed Labs
            var lab1 = new Lab { Name = "Riverside Pathology" };
            var lab2 = new Lab { Name = "Harbor Labs" };
            db.Labs.AddRange(lab1, lab2);
            db.SaveChanges(); // Save to generate Lab IDs (Lab1 will be 1, Lab2 will be 2)

            // Seed Clinics
            var clinic1 = new Clinic { LabId = lab1.Id, Name = "Riverside Clinic" };
            var clinic2 = new Clinic { LabId = lab2.Id, Name = "Harbor Clinic" };
            db.Clinics.AddRange(clinic1, clinic2);
            db.SaveChanges(); // Save to generate Clinic IDs (Clinic1 will be 1, Clinic2 will be 2)

            var now = DateTime.UtcNow;

            // ==================== LAB 1 DATA SEEDING ====================
            // Manifest 1: MAN-001 (Open)
            var m1 = new Manifest
            {
                LabId = lab1.Id,
                ClinicId = clinic1.Id,
                Code = "MAN-001",
                Status = ManifestStatus.Open,
                SentAt = now.AddHours(-24)
            };
            db.Manifests.Add(m1);
            db.SaveChanges(); // Generate Manifest ID

            var sp1 = new Specimen { LabId = lab1.Id, ManifestId = m1.Id, Code = "SP-001", Patient = "John Doe", Site = "Left Arm", Provider = "Dr. Smith", Status = SpecimenStatus.Received };
            var sp2 = new Specimen { LabId = lab1.Id, ManifestId = m1.Id, Code = "SP-002", Patient = "Jane Doe", Site = "Right Arm", Provider = "Dr. Smith", Status = SpecimenStatus.Received };
            var sp3 = new Specimen { LabId = lab1.Id, ManifestId = m1.Id, Code = "SP-003", Patient = "Bob Johnson", Site = "Urine", Provider = "Dr. Adams", Status = SpecimenStatus.Flagged };
            var sp4 = new Specimen { LabId = lab1.Id, ManifestId = m1.Id, Code = "SP-004", Patient = "Alice Williams", Site = "Throat Swab", Provider = "Dr. Adams", Status = SpecimenStatus.Added };
            var sp5 = new Specimen { LabId = lab1.Id, ManifestId = m1.Id, Code = "SP-005", Patient = "Charlie Brown", Site = "Nasal Swab", Provider = "Dr. Adams", Status = SpecimenStatus.Pending };
            db.Specimens.AddRange(sp1, sp2, sp3, sp4, sp5);
            db.SaveChanges(); // Generate Specimen IDs

            // Add events for Manifest 1
            db.CheckInEvents.AddRange(
                new CheckInEvent { LabId = lab1.Id, ManifestId = m1.Id, SpecimenId = sp1.Id, Action = "Received", UserId = "seed-user", At = now.AddMinutes(-50) },
                new CheckInEvent { LabId = lab1.Id, ManifestId = m1.Id, SpecimenId = sp2.Id, Action = "Received", UserId = "seed-user", At = now.AddMinutes(-45) },
                new CheckInEvent { LabId = lab1.Id, ManifestId = m1.Id, SpecimenId = sp3.Id, Action = "Flagged", UserId = "seed-user", At = now.AddMinutes(-40) },
                new CheckInEvent { LabId = lab1.Id, ManifestId = m1.Id, SpecimenId = sp4.Id, Action = "AddedOffManifest", UserId = "seed-user", At = now.AddMinutes(-30) }
            );

            // Add discrepancies for Manifest 1
            db.Discrepancies.AddRange(
                new Discrepancy { LabId = lab1.Id, ManifestId = m1.Id, SpecimenId = sp3.Id, Type = DiscrepancyType.Missing, Note = "Specimen flagged as missing during check-in", Status = DiscrepancyStatus.Open },
                new Discrepancy { LabId = lab1.Id, ManifestId = m1.Id, SpecimenId = sp4.Id, Type = DiscrepancyType.OffManifest, Note = "Off-manifest specimen added", Status = DiscrepancyStatus.Open }
            );

            // Manifest 2: MAN-002 (Open)
            var m2 = new Manifest
            {
                LabId = lab1.Id,
                ClinicId = clinic1.Id,
                Code = "MAN-002",
                Status = ManifestStatus.Open,
                SentAt = now.AddHours(-12)
            };
            db.Manifests.Add(m2);
            db.SaveChanges();

            var sp6 = new Specimen { LabId = lab1.Id, ManifestId = m2.Id, Code = "SP-006", Patient = "David Miller", Site = "Plasma", Provider = "Dr. Davis", Status = SpecimenStatus.Received };
            var sp7 = new Specimen { LabId = lab1.Id, ManifestId = m2.Id, Code = "SP-007", Patient = "Emily Davis", Site = "Serum", Provider = "Dr. Davis", Status = SpecimenStatus.Received };
            var sp8 = new Specimen { LabId = lab1.Id, ManifestId = m2.Id, Code = "SP-008", Patient = "Frank Wilson", Site = "Blood Swab", Provider = "Dr. Jones", Status = SpecimenStatus.Pending };
            var sp9 = new Specimen { LabId = lab1.Id, ManifestId = m2.Id, Code = "SP-009", Patient = "Grace Taylor", Site = "Urine", Provider = "Dr. Jones", Status = SpecimenStatus.Pending };
            var sp10 = new Specimen { LabId = lab1.Id, ManifestId = m2.Id, Code = "SP-010", Patient = "Henry Thomas", Site = "Saliva", Provider = "Dr. Jones", Status = SpecimenStatus.Pending };
            db.Specimens.AddRange(sp6, sp7, sp8, sp9, sp10);
            db.SaveChanges();

            db.CheckInEvents.AddRange(
                new CheckInEvent { LabId = lab1.Id, ManifestId = m2.Id, SpecimenId = sp6.Id, Action = "Received", UserId = "seed-user", At = now.AddMinutes(-20) },
                new CheckInEvent { LabId = lab1.Id, ManifestId = m2.Id, SpecimenId = sp7.Id, Action = "Received", UserId = "seed-user", At = now.AddMinutes(-15) }
            );

            // Manifest 3: MAN-003 (InTransit)
            var m3 = new Manifest
            {
                LabId = lab1.Id,
                ClinicId = clinic1.Id,
                Code = "MAN-003",
                Status = ManifestStatus.InTransit,
                SentAt = now.AddHours(-2)
            };
            db.Manifests.Add(m3);
            db.SaveChanges();

            var sp11 = new Specimen { LabId = lab1.Id, ManifestId = m3.Id, Code = "SP-011", Patient = "Irene White", Site = "Biopsy", Provider = "Dr. Evans", Status = SpecimenStatus.Pending };
            var sp12 = new Specimen { LabId = lab1.Id, ManifestId = m3.Id, Code = "SP-012", Patient = "Jack Black", Site = "Hair", Provider = "Dr. Evans", Status = SpecimenStatus.Pending };
            var sp13 = new Specimen { LabId = lab1.Id, ManifestId = m3.Id, Code = "SP-013", Patient = "Kelly Green", Site = "Nail", Provider = "Dr. Evans", Status = SpecimenStatus.Pending };
            var sp14 = new Specimen { LabId = lab1.Id, ManifestId = m3.Id, Code = "SP-014", Patient = "Leo Carter", Site = "Cerebrospinal Fluid", Provider = "Dr. Clark", Status = SpecimenStatus.Pending };
            var sp15 = new Specimen { LabId = lab1.Id, ManifestId = m3.Id, Code = "SP-015", Patient = "Mona Lisa", Site = "Stool", Provider = "Dr. Clark", Status = SpecimenStatus.Pending };
            db.Specimens.AddRange(sp11, sp12, sp13, sp14, sp15);


            // ==================== LAB 2 DATA SEEDING ====================
            // Manifest 4: MAN-004 (Open)
            var m4 = new Manifest
            {
                LabId = lab2.Id,
                ClinicId = clinic2.Id,
                Code = "MAN-004",
                Status = ManifestStatus.Open,
                SentAt = now.AddHours(-24)
            };
            db.Manifests.Add(m4);
            db.SaveChanges();

            var sp16 = new Specimen { LabId = lab2.Id, ManifestId = m4.Id, Code = "SP-016", Patient = "Nathan Drake", Site = "Left Arm", Provider = "Dr. Sullivan", Status = SpecimenStatus.Received };
            var sp17 = new Specimen { LabId = lab2.Id, ManifestId = m4.Id, Code = "SP-017", Patient = "Elena Fisher", Site = "Right Arm", Provider = "Dr. Sullivan", Status = SpecimenStatus.Received };
            var sp18 = new Specimen { LabId = lab2.Id, ManifestId = m4.Id, Code = "SP-018", Patient = "Victor Sullivan", Site = "Urine", Provider = "Dr. Croft", Status = SpecimenStatus.Flagged };
            var sp19 = new Specimen { LabId = lab2.Id, ManifestId = m4.Id, Code = "SP-019", Patient = "Lara Croft", Site = "Throat Swab", Provider = "Dr. Croft", Status = SpecimenStatus.Added };
            var sp20 = new Specimen { LabId = lab2.Id, ManifestId = m4.Id, Code = "SP-020", Patient = "Chloe Frazer", Site = "Nasal Swab", Provider = "Dr. Croft", Status = SpecimenStatus.Pending };
            db.Specimens.AddRange(sp16, sp17, sp18, sp19, sp20);
            db.SaveChanges();

            db.CheckInEvents.AddRange(
                new CheckInEvent { LabId = lab2.Id, ManifestId = m4.Id, SpecimenId = sp16.Id, Action = "Received", UserId = "seed-user", At = now.AddMinutes(-50) },
                new CheckInEvent { LabId = lab2.Id, ManifestId = m4.Id, SpecimenId = sp17.Id, Action = "Received", UserId = "seed-user", At = now.AddMinutes(-45) },
                new CheckInEvent { LabId = lab2.Id, ManifestId = m4.Id, SpecimenId = sp18.Id, Action = "Flagged", UserId = "seed-user", At = now.AddMinutes(-40) },
                new CheckInEvent { LabId = lab2.Id, ManifestId = m4.Id, SpecimenId = sp19.Id, Action = "AddedOffManifest", UserId = "seed-user", At = now.AddMinutes(-30) }
            );

            db.Discrepancies.AddRange(
                new Discrepancy { LabId = lab2.Id, ManifestId = m4.Id, SpecimenId = sp18.Id, Type = DiscrepancyType.Missing, Note = "Specimen flagged as missing during check-in", Status = DiscrepancyStatus.Open },
                new Discrepancy { LabId = lab2.Id, ManifestId = m4.Id, SpecimenId = sp19.Id, Type = DiscrepancyType.OffManifest, Note = "Off-manifest specimen added", Status = DiscrepancyStatus.Open }
            );

            // Manifest 5: MAN-005 (Open)
            var m5 = new Manifest
            {
                LabId = lab2.Id,
                ClinicId = clinic2.Id,
                Code = "MAN-005",
                Status = ManifestStatus.Open,
                SentAt = now.AddHours(-12)
            };
            db.Manifests.Add(m5);
            db.SaveChanges();

            var sp21 = new Specimen { LabId = lab2.Id, ManifestId = m5.Id, Code = "SP-021", Patient = "Peter Parker", Site = "Plasma", Provider = "Dr. Octavius", Status = SpecimenStatus.Received };
            var sp22 = new Specimen { LabId = lab2.Id, ManifestId = m5.Id, Code = "SP-022", Patient = "Mary Jane", Site = "Serum", Provider = "Dr. Octavius", Status = SpecimenStatus.Received };
            var sp23 = new Specimen { LabId = lab2.Id, ManifestId = m5.Id, Code = "SP-023", Patient = "Miles Morales", Site = "Blood Swab", Provider = "Dr. Osborn", Status = SpecimenStatus.Pending };
            var sp24 = new Specimen { LabId = lab2.Id, ManifestId = m5.Id, Code = "SP-024", Patient = "Gwen Stacy", Site = "Urine", Provider = "Dr. Osborn", Status = SpecimenStatus.Pending };
            var sp25 = new Specimen { LabId = lab2.Id, ManifestId = m5.Id, Code = "SP-025", Patient = "Harry Osborn", Site = "Saliva", Provider = "Dr. Osborn", Status = SpecimenStatus.Pending };
            db.Specimens.AddRange(sp21, sp22, sp23, sp24, sp25);
            db.SaveChanges();

            db.CheckInEvents.AddRange(
                new CheckInEvent { LabId = lab2.Id, ManifestId = m5.Id, SpecimenId = sp21.Id, Action = "Received", UserId = "seed-user", At = now.AddMinutes(-20) },
                new CheckInEvent { LabId = lab2.Id, ManifestId = m5.Id, SpecimenId = sp22.Id, Action = "Received", UserId = "seed-user", At = now.AddMinutes(-15) }
            );

            // Manifest 6: MAN-006 (InTransit)
            var m6 = new Manifest
            {
                LabId = lab2.Id,
                ClinicId = clinic2.Id,
                Code = "MAN-006",
                Status = ManifestStatus.InTransit,
                SentAt = now.AddHours(-2)
            };
            db.Manifests.Add(m6);
            db.SaveChanges();

            var sp26 = new Specimen { LabId = lab2.Id, ManifestId = m6.Id, Code = "SP-026", Patient = "Bruce Wayne", Site = "Biopsy", Provider = "Dr. Wayne", Status = SpecimenStatus.Pending };
            var sp27 = new Specimen { LabId = lab2.Id, ManifestId = m6.Id, Code = "SP-027", Patient = "Clark Kent", Site = "Hair", Provider = "Dr. Wayne", Status = SpecimenStatus.Pending };
            var sp28 = new Specimen { LabId = lab2.Id, ManifestId = m6.Id, Code = "SP-028", Patient = "Diana Prince", Site = "Nail", Provider = "Dr. Wayne", Status = SpecimenStatus.Pending };
            var sp29 = new Specimen { LabId = lab2.Id, ManifestId = m6.Id, Code = "SP-029", Patient = "Barry Allen", Site = "Cerebrospinal Fluid", Provider = "Dr. Irons", Status = SpecimenStatus.Pending };
            var sp30 = new Specimen { LabId = lab2.Id, ManifestId = m6.Id, Code = "SP-030", Patient = "Hal Jordan", Site = "Stool", Provider = "Dr. Irons", Status = SpecimenStatus.Pending };
            db.Specimens.AddRange(sp26, sp27, sp28, sp29, sp30);

            db.SaveChanges();
        }
    }
}
