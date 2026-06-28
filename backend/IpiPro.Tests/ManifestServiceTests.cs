using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using IpiPro.Api.Data;
using IpiPro.Api.DTOs;
using IpiPro.Api.Models;
using IpiPro.Api.Services;

namespace IpiPro.Tests
{
    public class ManifestServiceTests
    {
        private AppDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task Test1_ReceiveSpecimenIsIdempotent()
        {
            // Arrange
            var db = GetInMemoryContext();
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            // Seed a manifest with a specimen that is already Received
            var manifest = new Manifest { Id = 1, LabId = 1, ClinicId = 1, Code = "M-001", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow };
            var specimen = new Specimen { Id = 10, LabId = 1, ManifestId = 1, Code = "SP-01", Status = SpecimenStatus.Received };
            db.Manifests.Add(manifest);
            db.Specimens.Add(specimen);
            
            // Seed 1 event for the initial received state
            var ev = new CheckInEvent { Id = 100, LabId = 1, ManifestId = 1, SpecimenId = 10, Action = "Received", UserId = "seed-user", At = DateTime.UtcNow };
            db.CheckInEvents.Add(ev);
            
            await db.SaveChangesAsync();

            var service = new ManifestService(db, tenantMock.Object);

            // Act: Call ReceiveSpecimenAsync twice
            var res1 = await service.ReceiveSpecimenAsync(1, 10);
            var res2 = await service.ReceiveSpecimenAsync(1, 10);

            // Assert
            Assert.NotNull(res1);
            Assert.NotNull(res2);
            Assert.Equal("Received", res1.Status);
            Assert.Equal("Received", res2.Status);

            // Check that CheckInEvents count remains exactly 1 (no new event was inserted)
            var eventCount = await db.CheckInEvents.CountAsync(e => e.SpecimenId == 10 && e.Action == "Received");
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public async Task Test2_CloseManifestRejectedWhenPendingSpecimensExist()
        {
            // Arrange
            var db = GetInMemoryContext();
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            // Seed a manifest with 1 pending specimen
            var lab = new Lab { Id = 1, Name = "Lab 1" };
            var clinic = new Clinic { Id = 1, LabId = 1, Name = "Clinic 1" };
            var manifest = new Manifest { Id = 1, LabId = 1, ClinicId = 1, Code = "M-001", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow };
            var specimen = new Specimen { Id = 10, LabId = 1, ManifestId = 1, Code = "SP-01", Status = SpecimenStatus.Pending, Manifest = manifest };
            manifest.Specimens.Add(specimen);
            
            db.Labs.Add(lab);
            db.Clinics.Add(clinic);
            db.Manifests.Add(manifest);
            await db.SaveChangesAsync();

            var service = new ManifestService(db, tenantMock.Object);

            // Act & Assert: Call CloseManifestAsync and verify it throws InvalidOperationException
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CloseManifestAsync(1));
            Assert.Equal("Cannot close manifest with pending specimens", exception.Message);
        }

        [Fact]
        public async Task Test3_FlagSpecimenCreatesMissingDiscrepancy()
        {
            // Arrange
            var db = GetInMemoryContext();
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            // Seed a manifest with 1 Pending specimen
            var manifest = new Manifest { Id = 1, LabId = 1, ClinicId = 1, Code = "M-001", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow };
            var specimen = new Specimen { Id = 10, LabId = 1, ManifestId = 1, Code = "SP-01", Status = SpecimenStatus.Pending };
            db.Manifests.Add(manifest);
            db.Specimens.Add(specimen);
            await db.SaveChangesAsync();

            var service = new ManifestService(db, tenantMock.Object);

            // Act: Call FlagSpecimenAsync
            var result = await service.FlagSpecimenAsync(1, 10);

            // Assert: Status should be Flagged
            Assert.NotNull(result);
            Assert.Equal("Flagged", result.Status);

            // Verify a discrepancy of Type Missing and Status Open was created
            var discrepancy = await db.Discrepancies.FirstOrDefaultAsync(d => d.SpecimenId == 10);
            Assert.NotNull(discrepancy);
            Assert.Equal(DiscrepancyType.Missing, discrepancy.Type);
            Assert.Equal(DiscrepancyStatus.Open, discrepancy.Status);
            Assert.Equal(1, discrepancy.LabId);
        }

        [Fact]
        public async Task Test4_AddOffManifestSpecimenCreatesAddedSpecimenAndOffManifestDiscrepancy()
        {
            // Arrange
            var db = GetInMemoryContext();
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            // Seed a manifest
            var manifest = new Manifest { Id = 1, LabId = 1, ClinicId = 1, Code = "M-001", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow };
            db.Manifests.Add(manifest);
            await db.SaveChangesAsync();

            var service = new ManifestService(db, tenantMock.Object);

            // Act: Call AddOffManifestSpecimenAsync
            var input = new SpecimenInputDto
            {
                Code = "SP-OFF-01",
                Patient = "Logan",
                Site = "Saliva",
                Provider = "Dr. Xavier"
            };
            var result = await service.AddOffManifestSpecimenAsync(1, input);

            // Assert: Specimen created and returned as Added
            Assert.NotNull(result);
            Assert.Equal("SP-OFF-01", result.Code);
            Assert.Equal("Added", result.Status);

            // Verify specimen exists in DB
            var dbSpecimen = await db.Specimens.FirstOrDefaultAsync(s => s.Code == "SP-OFF-01" && s.ManifestId == 1);
            Assert.NotNull(dbSpecimen);
            Assert.Equal(SpecimenStatus.Added, dbSpecimen.Status);

            // Verify a discrepancy of Type OffManifest and Status Open was created for the new specimen
            var discrepancy = await db.Discrepancies.FirstOrDefaultAsync(d => d.SpecimenId == dbSpecimen.Id);
            Assert.NotNull(discrepancy);
            Assert.Equal(DiscrepancyType.OffManifest, discrepancy.Type);
            Assert.Equal(DiscrepancyStatus.Open, discrepancy.Status);
        }

        [Fact]
        public async Task Test5_CloseManifestRejectedWhenOpenDiscrepanciesExist()
        {
            // Arrange
            var db = GetInMemoryContext();
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            var lab = new Lab { Id = 1, Name = "Lab 1" };
            var clinic = new Clinic { Id = 1, LabId = 1, Name = "Clinic 1" };
            var manifest = new Manifest { Id = 1, LabId = 1, ClinicId = 1, Code = "M-001", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow };
            var specimen = new Specimen { Id = 10, LabId = 1, ManifestId = 1, Code = "SP-01", Status = SpecimenStatus.Flagged, Manifest = manifest };
            var discrepancy = new Discrepancy { Id = 100, LabId = 1, ManifestId = 1, SpecimenId = 10, Type = DiscrepancyType.Missing, Status = DiscrepancyStatus.Open, Note = "Missing specimen" };
            
            manifest.Specimens.Add(specimen);
            manifest.Discrepancies.Add(discrepancy);
            
            db.Labs.Add(lab);
            db.Clinics.Add(clinic);
            db.Manifests.Add(manifest);
            await db.SaveChangesAsync();

            var service = new ManifestService(db, tenantMock.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CloseManifestAsync(1));
            Assert.Equal("Cannot close manifest while discrepancies remain open.", exception.Message);
        }

        [Fact]
        public async Task Test6_CloseManifestSucceedsWhenAllDiscrepanciesAreResolved()
        {
            // Arrange
            var db = GetInMemoryContext();
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            var lab = new Lab { Id = 1, Name = "Lab 1" };
            var clinic = new Clinic { Id = 1, LabId = 1, Name = "Clinic 1" };
            var manifest = new Manifest { Id = 1, LabId = 1, ClinicId = 1, Code = "M-001", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow };
            var specimen = new Specimen { Id = 10, LabId = 1, ManifestId = 1, Code = "SP-01", Status = SpecimenStatus.Flagged, Manifest = manifest };
            var discrepancy = new Discrepancy { Id = 100, LabId = 1, ManifestId = 1, SpecimenId = 10, Type = DiscrepancyType.Missing, Status = DiscrepancyStatus.Resolved, Note = "Missing specimen", ResolutionNote = "Resolved" };
            
            manifest.Specimens.Add(specimen);
            manifest.Discrepancies.Add(discrepancy);
            
            db.Labs.Add(lab);
            db.Clinics.Add(clinic);
            db.Manifests.Add(manifest);
            await db.SaveChangesAsync();

            var service = new ManifestService(db, tenantMock.Object);

            // Act
            var result = await service.CloseManifestAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Closed", result.Status);
        }

        [Fact]
        public async Task Test7_ResolveDiscrepancyUpdatesStatusAndNote()
        {
            // Arrange
            var db = GetInMemoryContext();
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            var manifest = new Manifest { Id = 1, LabId = 1, ClinicId = 1, Code = "M-001", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow };
            var specimen = new Specimen { Id = 10, LabId = 1, ManifestId = 1, Code = "SP-01", Status = SpecimenStatus.Flagged };
            var discrepancy = new Discrepancy { Id = 100, LabId = 1, ManifestId = 1, SpecimenId = 10, Type = DiscrepancyType.Missing, Status = DiscrepancyStatus.Open, Note = "Missing specimen" };
            
            db.Manifests.Add(manifest);
            db.Specimens.Add(specimen);
            db.Discrepancies.Add(discrepancy);
            await db.SaveChangesAsync();

            var service = new ManifestService(db, tenantMock.Object);

            // Act
            var result = await service.ResolveDiscrepancyAsync(1, 100, "Found in second box");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Resolved", result.Status);
            Assert.Equal("Found in second box", result.ResolutionNote);
            Assert.NotNull(result.ResolvedAt);

            // Verify db updates
            var dbDiscrepancy = await db.Discrepancies.FindAsync(100);
            Assert.NotNull(dbDiscrepancy);
            Assert.Equal(DiscrepancyStatus.Resolved, dbDiscrepancy.Status);
            Assert.Equal("Found in second box", dbDiscrepancy.ResolutionNote);

            // Verify CheckInEvent created
            var hasEvent = await db.CheckInEvents.AnyAsync(e => e.SpecimenId == 10 && e.Action == "ResolvedDiscrepancy");
            Assert.True(hasEvent);
        }
    }
}
