using Microsoft.EntityFrameworkCore;
using IpiPro.Api.Models;

namespace IpiPro.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Lab> Labs => Set<Lab>();
        public DbSet<Clinic> Clinics => Set<Clinic>();
        public DbSet<Manifest> Manifests => Set<Manifest>();
        public DbSet<Specimen> Specimens => Set<Specimen>();
        public DbSet<CheckInEvent> CheckInEvents => Set<CheckInEvent>();
        public DbSet<Discrepancy> Discrepancies => Set<Discrepancy>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Lab
            modelBuilder.Entity<Lab>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            });

            // Clinic
            modelBuilder.Entity<Clinic>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();

                entity.HasOne(e => e.Lab)
                    .WithMany(l => l.Clinics)
                    .HasForeignKey(e => e.LabId)
                    .OnDelete(DeleteBehavior.Cascade); // Lab -> Clinics cascade delete
            });

            // Manifest
            modelBuilder.Entity<Manifest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Status).HasConversion<int>().IsRequired();
                entity.Property(e => e.SentAt).HasColumnType("datetime2").IsRequired();

                entity.HasIndex(e => e.LabId); // Index on Manifest.LabId

                entity.HasOne(e => e.Lab)
                    .WithMany(l => l.Manifests)
                    .HasForeignKey(e => e.LabId)
                    .OnDelete(DeleteBehavior.Cascade); // Lab -> Manifests cascade delete

                entity.HasOne(e => e.Clinic)
                    .WithMany(c => c.Manifests)
                    .HasForeignKey(e => e.ClinicId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Specimen
            modelBuilder.Entity<Specimen>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Patient).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Site).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Provider).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Status).HasConversion<int>().IsRequired();

                entity.HasIndex(e => e.ManifestId); // Index on Specimen.ManifestId

                entity.HasOne(e => e.Manifest)
                    .WithMany(m => m.Specimens)
                    .HasForeignKey(e => e.ManifestId)
                    .OnDelete(DeleteBehavior.Cascade); // Manifest -> Specimens cascade delete

                entity.HasOne(e => e.Lab)
                    .WithMany()
                    .HasForeignKey(e => e.LabId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // CheckInEvent
            modelBuilder.Entity<CheckInEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
                entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.At).HasColumnType("datetime2").IsRequired();

                entity.HasIndex(e => e.ManifestId); // Index on CheckInEvent.ManifestId

                entity.HasOne(e => e.Manifest)
                    .WithMany(m => m.CheckInEvents)
                    .HasForeignKey(e => e.ManifestId)
                    .OnDelete(DeleteBehavior.Cascade); // Manifest -> CheckInEvents cascade delete

                entity.HasOne(e => e.Specimen)
                    .WithMany()
                    .HasForeignKey(e => e.SpecimenId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Lab)
                    .WithMany()
                    .HasForeignKey(e => e.LabId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Discrepancy
            modelBuilder.Entity<Discrepancy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasConversion<int>().IsRequired();
                entity.Property(e => e.Note).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Status).HasConversion<int>().IsRequired();
                entity.Property(e => e.ResolutionNote).HasMaxLength(500);
                entity.Property(e => e.ResolvedAt).HasColumnType("datetime2");

                entity.HasIndex(e => e.ManifestId); // Index on Discrepancy.ManifestId

                entity.HasOne(e => e.Manifest)
                    .WithMany(m => m.Discrepancies)
                    .HasForeignKey(e => e.ManifestId)
                    .OnDelete(DeleteBehavior.Cascade); // Manifest -> Discrepancies cascade delete

                entity.HasOne(e => e.Specimen)
                    .WithMany()
                    .HasForeignKey(e => e.SpecimenId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Lab)
                    .WithMany()
                    .HasForeignKey(e => e.LabId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
