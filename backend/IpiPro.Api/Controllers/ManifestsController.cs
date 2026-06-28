using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using IpiPro.Api.DTOs;
using IpiPro.Api.Services;

namespace IpiPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManifestsController : ControllerBase
    {
        private readonly ManifestService _manifestService;
        private readonly ILogger<ManifestsController> _logger;

        public ManifestsController(ManifestService manifestService, ILogger<ManifestsController> logger)
        {
            _manifestService = manifestService;
            _logger = logger;
        }

        // GET /api/manifests?status=Open
        [HttpGet]
        public async Task<IActionResult> GetManifests([FromQuery] string? status)
        {
            try
            {
                var manifests = await _manifestService.GetManifestsAsync(status);
                return Ok(manifests);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, new { error = ex.Message, code = "Unauthorized" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetManifests");
                return StatusCode(500, new { error = "An unexpected error occurred", code = "InternalServerError" });
            }
        }

        // GET /api/manifests/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetManifestById(int id)
        {
            try
            {
                var manifest = await _manifestService.GetManifestByIdAsync(id);
                if (manifest == null)
                {
                    return NotFound(new { error = $"Manifest with ID {id} was not found or is access-denied", code = "NotFound" });
                }
                return Ok(manifest);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, new { error = ex.Message, code = "Unauthorized" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetManifestById for ID {Id}", id);
                return StatusCode(500, new { error = "An unexpected error occurred", code = "InternalServerError" });
            }
        }

        // POST /api/manifests/{id}/specimens/{sid}/receive
        [HttpPost("{id}/specimens/{sid}/receive")]
        public async Task<IActionResult> ReceiveSpecimen(int id, int sid)
        {
            try
            {
                var specimen = await _manifestService.ReceiveSpecimenAsync(id, sid);
                if (specimen == null)
                {
                    return NotFound(new { error = $"Specimen with ID {sid} on Manifest {id} was not found or is access-denied", code = "NotFound" });
                }
                return Ok(specimen);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message, code = "BadRequest" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, new { error = ex.Message, code = "Unauthorized" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ReceiveSpecimen for Manifest {ManifestId}, Specimen {SpecimenId}", id, sid);
                return StatusCode(500, new { error = "An unexpected error occurred", code = "InternalServerError" });
            }
        }

        // POST /api/manifests/{id}/specimens/{sid}/flag
        [HttpPost("{id}/specimens/{sid}/flag")]
        public async Task<IActionResult> FlagSpecimen(int id, int sid)
        {
            try
            {
                var specimen = await _manifestService.FlagSpecimenAsync(id, sid);
                if (specimen == null)
                {
                    return NotFound(new { error = $"Specimen with ID {sid} on Manifest {id} was not found or is access-denied", code = "NotFound" });
                }
                return Ok(specimen);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message, code = "BadRequest" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, new { error = ex.Message, code = "Unauthorized" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in FlagSpecimen for Manifest {ManifestId}, Specimen {SpecimenId}", id, sid);
                return StatusCode(500, new { error = "An unexpected error occurred", code = "InternalServerError" });
            }
        }

        // POST /api/manifests/{id}/specimens
        [HttpPost("{id}/specimens")]
        public async Task<IActionResult> AddOffManifestSpecimen(int id, [FromBody] SpecimenInputDto input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid input data", code = "BadRequest" });
            }

            try
            {
                var specimen = await _manifestService.AddOffManifestSpecimenAsync(id, input);
                if (specimen == null)
                {
                    return NotFound(new { error = $"Manifest with ID {id} was not found or is access-denied", code = "NotFound" });
                }
                return Ok(specimen);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message, code = "BadRequest" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, new { error = ex.Message, code = "Unauthorized" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AddOffManifestSpecimen for Manifest {ManifestId}", id);
                return StatusCode(500, new { error = "An unexpected error occurred", code = "InternalServerError" });
            }
        }

        // POST /api/manifests/{id}/close
        [HttpPost("{id}/close")]
        public async Task<IActionResult> CloseManifest(int id)
        {
            try
            {
                var manifest = await _manifestService.CloseManifestAsync(id);
                if (manifest == null)
                {
                    return NotFound(new { error = $"Manifest with ID {id} was not found or is access-denied", code = "NotFound" });
                }
                return Ok(manifest);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message, code = "ManifestNotReconciled" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, new { error = ex.Message, code = "Unauthorized" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CloseManifest for Manifest {ManifestId}", id);
                return StatusCode(500, new { error = "An unexpected error occurred", code = "InternalServerError" });
            }
        }

        // POST /api/manifests/{id}/discrepancies/{did}/resolve
        [HttpPost("{id}/discrepancies/{did}/resolve")]
        public async Task<IActionResult> ResolveDiscrepancy(int id, int did, [FromBody] ResolveDiscrepancyInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid input data", code = "BadRequest" });
            }

            try
            {
                var discrepancy = await _manifestService.ResolveDiscrepancyAsync(id, did, input.Note);
                if (discrepancy == null)
                {
                    return NotFound(new { error = $"Discrepancy with ID {did} on Manifest {id} was not found or is access-denied", code = "NotFound" });
                }
                return Ok(discrepancy);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message, code = "BadRequest" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, new { error = ex.Message, code = "Unauthorized" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ResolveDiscrepancy for Manifest {ManifestId}, Discrepancy {DiscrepancyId}", id, did);
                return StatusCode(500, new { error = "An unexpected error occurred", code = "InternalServerError" });
            }
        }
    }
}

