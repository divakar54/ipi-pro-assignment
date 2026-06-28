using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using IpiPro.Api.Data;
using IpiPro.Api.Models;
using IpiPro.Api.Services;

namespace IpiPro.Tests
{
    public class TenantIsolationTests
    {
        private AppDbContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            
            // Seed base data representing both Lab 1 and Lab 2
            SeedDataForTests(context);
            return context;
        }

        private void SeedDataForTests(AppDbContext db)
        {
            // Labs
            var lab1 = new Lab { Id = 1, Name = "Riverside Pathology" };
            var lab2 = new Lab { Id = 2, Name = "Harbor Labs" };
            db.Labs.AddRange(lab1, lab2);

            // Clinics
            var clinic1 = new Clinic { Id = 1, LabId = 1, Name = "Riverside Clinic" };
            var clinic2 = new Clinic { Id = 2, LabId = 2, Name = "Harbor Clinic" };
            db.Clinics.AddRange(clinic1, clinic2);

            // Lab 1 Manifests
            db.Manifests.AddRange(
                new Manifest { Id = 101, LabId = 1, ClinicId = 1, Code = "MAN-001", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow.AddHours(-1) },
                new Manifest { Id = 102, LabId = 1, ClinicId = 1, Code = "MAN-002", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow.AddHours(-2) },
                new Manifest { Id = 103, LabId = 1, ClinicId = 1, Code = "MAN-003", Status = ManifestStatus.InTransit, SentAt = DateTime.UtcNow.AddHours(-3) }
            );

            // Lab 2 Manifests
            db.Manifests.AddRange(
                new Manifest { Id = 201, LabId = 2, ClinicId = 2, Code = "MAN-004", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow.AddHours(-1) },
                new Manifest { Id = 202, LabId = 2, ClinicId = 2, Code = "MAN-005", Status = ManifestStatus.Open, SentAt = DateTime.UtcNow.AddHours(-2) },
                new Manifest { Id = 203, LabId = 2, ClinicId = 2, Code = "MAN-006", Status = ManifestStatus.InTransit, SentAt = DateTime.UtcNow.AddHours(-3) }
            );

            db.SaveChanges();
        }

        [Fact]
        public async Task Test1_CrossTenantManifestReadIsBlocked()
        {
            // Arrange: Tenant is LabId = 1
            var db = GetInMemoryContext(Guid.NewGuid().ToString());
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            var service = new ManifestService(db, tenantMock.Object);

            // Act: Try to fetch Lab 2 manifest (ID = 201)
            var manifest = await service.GetManifestByIdAsync(201);

            // Assert: Should return null because it belongs to Lab 2
            Assert.Null(manifest);
        }

        [Fact]
        public async Task Test2_ListReturnsOnlyCurrentTenantsManifests()
        {
            // Arrange: Tenant is LabId = 1
            var db = GetInMemoryContext(Guid.NewGuid().ToString());
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            var service = new ManifestService(db, tenantMock.Object);

            // Act: Get manifests list
            var results = await service.GetManifestsAsync(null);

            // Assert: Should return exactly 3 manifests, all belonging to Lab 1
            Assert.Equal(3, results.Count);
            Assert.All(results, m => Assert.Equal(1, m.LabId));
        }

        [Fact]
        public async Task Test3_TenantMutationProtection_CrossTenantReceivingBlocked()
        {
            // Arrange: Tenant is LabId = 1
            var db = GetInMemoryContext(Guid.NewGuid().ToString());
            var tenantMock = new Mock<ITenantService>();
            tenantMock.Setup(t => t.LabId).Returns(1);

            // Seed a Lab 2 manifest (ID = 201) and specimen (ID = 210) belonging to Lab 2
            var manifest = await db.Manifests.FindAsync(201);
            var specimen = new Specimen { Id = 210, LabId = 2, ManifestId = 201, Code = "SP-L2", Status = SpecimenStatus.Pending };
            db.Specimens.Add(specimen);
            await db.SaveChangesAsync();

            var service = new ManifestService(db, tenantMock.Object);

            // Act: Lab 1 tries to receive Lab 2's specimen
            var result = await service.ReceiveSpecimenAsync(201, 210);

            // Assert: Should return null because it belongs to Lab 2
            Assert.Null(result);

            // Verify status in DB remains Pending
            var dbSpecimen = await db.Specimens.FindAsync(210);
            Assert.NotNull(dbSpecimen);
            Assert.Equal(SpecimenStatus.Pending, dbSpecimen.Status);
        }

        [Fact]
        public void Test4_TenantIsolationInvalidHeaderVerification()
        {
            // Arrange: Setup HttpContextAccessor with a request that is missing the header
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            var contextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            contextAccessorMock.Setup(c => c.HttpContext).Returns(httpContext);

            var tenantService = new TenantService(contextAccessorMock.Object);

            // Act & Assert: missing header should throw UnauthorizedAccessException
            var exception = Assert.Throws<UnauthorizedAccessException>(() => tenantService.LabId);
            Assert.Equal("X-Lab-Id header is missing or invalid", exception.Message);

            // Arrange: Setup header with non-integer value
            httpContext.Request.Headers["X-Lab-Id"] = "invalid-lab-id";

            // Act & Assert: invalid header should throw UnauthorizedAccessException
            var exception2 = Assert.Throws<UnauthorizedAccessException>(() => tenantService.LabId);
            Assert.Equal("X-Lab-Id header is missing or invalid", exception2.Message);
        }
    }
}

