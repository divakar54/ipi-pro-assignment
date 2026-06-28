using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IpiPro.Api.Data;
using IpiPro.Api.DTOs;
using IpiPro.Api.Models;

namespace IpiPro.Api.Services
{
    public class ManifestService
    {
        private readonly AppDbContext _db;
        private readonly ITenantService _tenant;

        public ManifestService(AppDbContext db, ITenantService tenant)
        {
            _db = db;
            _tenant = tenant;
        }

        public async Task<List<ManifestDto>> GetManifestsAsync(string? status)
        {
            var query = _db.Manifests
                .Include(m => m.Clinic)
                .Include(m => m.Specimens)
                .Include(m => m.CheckInEvents)
                .Where(m => m.LabId == _tenant.LabId);

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<ManifestStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(m => m.Status == parsedStatus);
                }
                else
                {
                    return new List<ManifestDto>();
                }
            }

            var manifests = await query
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return manifests.Select(m => MapToManifestDto(m)).ToList();
        }

        public async Task<ManifestDto?> GetManifestByIdAsync(int id)
        {
            var manifest = await _db.Manifests
                .Include(m => m.Clinic)
                .Include(m => m.Specimens)
                .Include(m => m.Discrepancies)
                .Include(m => m.CheckInEvents)
                .FirstOrDefaultAsync(m => m.Id == id && m.LabId == _tenant.LabId);

            if (manifest == null)
            {
                return null;
            }

            return MapToManifestDto(manifest);
        }

        public async Task<SpecimenDto?> ReceiveSpecimenAsync(int manifestId, int specimenId)
        {
            var manifestExists = await _db.Manifests
                .AnyAsync(m => m.Id == manifestId && m.LabId == _tenant.LabId);
            if (!manifestExists)
            {
                return null;
            }

            var specimen = await _db.Specimens
                .FirstOrDefaultAsync(s => s.Id == specimenId && s.ManifestId == manifestId && s.LabId == _tenant.LabId);
            if (specimen == null)
            {
                return null;
            }

            if (specimen.Status == SpecimenStatus.Received)
            {
                var existingEvent = await _db.CheckInEvents
                    .FirstOrDefaultAsync(e => e.SpecimenId == specimenId && e.Action == "Received" && e.LabId == _tenant.LabId);
                return MapToSpecimenDto(specimen, existingEvent != null ? new[] { existingEvent } : null);
            }

            specimen.Status = SpecimenStatus.Received;

            var checkInEvent = new CheckInEvent
            {
                LabId = _tenant.LabId,
                ManifestId = manifestId,
                SpecimenId = specimenId,
                Action = "Received",
                UserId = "current-user",
                At = DateTime.UtcNow
            };
            _db.CheckInEvents.Add(checkInEvent);

            await _db.SaveChangesAsync();

            return MapToSpecimenDto(specimen, new[] { checkInEvent });
        }

        public async Task<SpecimenDto?> FlagSpecimenAsync(int manifestId, int specimenId)
        {
            var manifestExists = await _db.Manifests
                .AnyAsync(m => m.Id == manifestId && m.LabId == _tenant.LabId);
            if (!manifestExists)
            {
                return null;
            }

            var specimen = await _db.Specimens
                .FirstOrDefaultAsync(s => s.Id == specimenId && s.ManifestId == manifestId && s.LabId == _tenant.LabId);
            if (specimen == null)
            {
                return null;
            }

            if (specimen.Status != SpecimenStatus.Pending)
            {
                throw new InvalidOperationException("Only pending specimens can be flagged as missing.");
            }

            specimen.Status = SpecimenStatus.Flagged;

            var discrepancy = new Discrepancy
            {
                LabId = _tenant.LabId,
                ManifestId = manifestId,
                SpecimenId = specimenId,
                Type = DiscrepancyType.Missing,
                Note = "Specimen flagged as missing during check-in",
                Status = DiscrepancyStatus.Open
            };
            _db.Discrepancies.Add(discrepancy);

            var checkInEvent = new CheckInEvent
            {
                LabId = _tenant.LabId,
                ManifestId = manifestId,
                SpecimenId = specimenId,
                Action = "Flagged",
                UserId = "current-user",
                At = DateTime.UtcNow
            };
            _db.CheckInEvents.Add(checkInEvent);

            await _db.SaveChangesAsync();

            return MapToSpecimenDto(specimen, new[] { checkInEvent });
        }

        public async Task<SpecimenDto?> AddOffManifestSpecimenAsync(int manifestId, SpecimenInputDto input)
        {
            var manifestExists = await _db.Manifests
                .AnyAsync(m => m.Id == manifestId && m.LabId == _tenant.LabId);
            if (!manifestExists)
            {
                return null;
            }

            var specimen = new Specimen
            {
                LabId = _tenant.LabId,
                ManifestId = manifestId,
                Code = input.Code,
                Patient = input.Patient,
                Site = input.Site,
                Provider = input.Provider,
                Status = SpecimenStatus.Added
            };
            _db.Specimens.Add(specimen);
            await _db.SaveChangesAsync();

            var discrepancy = new Discrepancy
            {
                LabId = _tenant.LabId,
                ManifestId = manifestId,
                SpecimenId = specimen.Id,
                Type = DiscrepancyType.OffManifest,
                Note = "Off-manifest specimen added",
                Status = DiscrepancyStatus.Open
            };
            _db.Discrepancies.Add(discrepancy);

            var checkInEvent = new CheckInEvent
            {
                LabId = _tenant.LabId,
                ManifestId = manifestId,
                SpecimenId = specimen.Id,
                Action = "AddedOffManifest",
                UserId = "current-user",
                At = DateTime.UtcNow
            };
            _db.CheckInEvents.Add(checkInEvent);

            await _db.SaveChangesAsync();

            return MapToSpecimenDto(specimen, new[] { checkInEvent });
        }

        public async Task<DiscrepancyDto?> ResolveDiscrepancyAsync(int manifestId, int discrepancyId, string note)
        {
            var manifestExists = await _db.Manifests
                .AnyAsync(m => m.Id == manifestId && m.LabId == _tenant.LabId);
            if (!manifestExists)
            {
                return null;
            }

            var discrepancy = await _db.Discrepancies
                .Include(d => d.Specimen)
                .FirstOrDefaultAsync(d => d.Id == discrepancyId && d.ManifestId == manifestId && d.LabId == _tenant.LabId);

            if (discrepancy == null)
            {
                return null;
            }

            discrepancy.Status = DiscrepancyStatus.Resolved;
            discrepancy.ResolutionNote = note;
            discrepancy.ResolvedAt = DateTime.UtcNow;

            var checkInEvent = new CheckInEvent
            {
                LabId = _tenant.LabId,
                ManifestId = manifestId,
                SpecimenId = discrepancy.SpecimenId,
                Action = "ResolvedDiscrepancy",
                UserId = "current-user",
                At = DateTime.UtcNow
            };
            _db.CheckInEvents.Add(checkInEvent);

            await _db.SaveChangesAsync();

            return MapToDiscrepancyDto(discrepancy);
        }

        public async Task<ManifestDto?> CloseManifestAsync(int manifestId)
        {
            var manifest = await _db.Manifests
                .Include(m => m.Clinic)
                .Include(m => m.Specimens)
                .Include(m => m.Discrepancies)
                .Include(m => m.CheckInEvents)
                .FirstOrDefaultAsync(m => m.Id == manifestId && m.LabId == _tenant.LabId);

            if (manifest == null)
            {
                return null;
            }

            var hasPending = manifest.Specimens.Any(s => s.Status == SpecimenStatus.Pending);
            if (hasPending)
            {
                throw new InvalidOperationException("Cannot close manifest with pending specimens");
            }

            var hasOpenDiscrepancies = manifest.Discrepancies.Any(d => d.Status == DiscrepancyStatus.Open);
            if (hasOpenDiscrepancies)
            {
                throw new InvalidOperationException("Cannot close manifest while discrepancies remain open.");
            }

            manifest.Status = ManifestStatus.Closed;

            var checkInEvent = new CheckInEvent
            {
                LabId = _tenant.LabId,
                ManifestId = manifestId,
                SpecimenId = null,
                Action = "Closed",
                UserId = "current-user",
                At = DateTime.UtcNow
            };
            _db.CheckInEvents.Add(checkInEvent);

            await _db.SaveChangesAsync();

            return MapToManifestDto(manifest);
        }

        private static ManifestDto MapToManifestDto(Manifest m)
        {
            return new ManifestDto
            {
                Id = m.Id,
                LabId = m.LabId,
                Code = m.Code,
                Status = m.Status.ToString(),
                ClinicName = m.Clinic?.Name ?? string.Empty,
                SentAt = m.SentAt,
                PendingCount = m.Specimens.Count(s => s.Status == SpecimenStatus.Pending),
                ReceivedCount = m.Specimens.Count(s => s.Status == SpecimenStatus.Received),
                FlaggedCount = m.Specimens.Count(s => s.Status == SpecimenStatus.Flagged),
                AddedCount = m.Specimens.Count(s => s.Status == SpecimenStatus.Added),
                Specimens = m.Specimens.Select(s => MapToSpecimenDto(s, m.CheckInEvents)).ToList(),
                Discrepancies = m.Discrepancies.Select(d => MapToDiscrepancyDto(d)).ToList()
            };
        }

        private static SpecimenDto MapToSpecimenDto(Specimen s, IEnumerable<CheckInEvent>? events = null)
        {
            var ev = events?.FirstOrDefault(e => e.SpecimenId == s.Id && (e.Action == "Received" || e.Action == "AddedOffManifest"));
            return new SpecimenDto
            {
                Id = s.Id,
                ManifestId = s.ManifestId,
                Code = s.Code,
                Patient = s.Patient,
                Site = s.Site,
                Provider = s.Provider,
                Status = s.Status.ToString(),
                ReceivedBy = ev?.UserId ?? string.Empty,
                ReceivedAt = ev?.At
            };
        }

        private static DiscrepancyDto MapToDiscrepancyDto(Discrepancy d)
        {
            return new DiscrepancyDto
            {
                Id = d.Id,
                ManifestId = d.ManifestId,
                SpecimenId = d.SpecimenId,
                Type = d.Type.ToString(),
                Note = d.Note,
                Status = d.Status.ToString(),
                ResolutionNote = d.ResolutionNote,
                ResolvedAt = d.ResolvedAt
            };
        }
    }
}
