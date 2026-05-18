using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serwer.Extensions;

namespace Serwer.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    [Authorize]
    public class PriceAlertController : ControllerBase
    {
        private readonly IPriceAlertService _alertService;

        public PriceAlertController(IPriceAlertService alertService)
        {
            _alertService = alertService;
        }

        private Guid UserId => User.GetUserId();

        /// <summary>
        /// Get all alerts for the authenticated user.
        /// </summary>
        /// <returns>List of all alerts (active and triggered)</returns>
        /// <response code="200">Returns list of alerts</response>
        /// <response code="401">Unauthorized - missing or invalid JWT token</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAlerts()
        {
            var alerts = await _alertService.GetUserAlertsAsync(UserId);
            return Ok(alerts);
        }

        /// <summary>
        /// Get a specific alert by ID.
        /// </summary>
        /// <param name="id">The alert ID (GUID)</param>
        /// <returns>Alert details</returns>
        /// <response code="200">Returns the alert</response>
        /// <response code="401">Unauthorized - missing or invalid JWT token</response>
        /// <response code="403">Forbidden - alert belongs to a different user</response>
        /// <response code="404">Not Found - alert does not exist</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAlertById(Guid id)
        {
            try
            {
                var alert = await _alertService.GetAlertByIdAsync(UserId, id);
                return Ok(alert);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Alert not found" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Create a new price alert.
        /// </summary>
        /// <param name="dto">Alert creation data (Symbol, TargetPrice, Direction)</param>
        /// <returns>The created alert</returns>
        /// <response code="201">Alert created successfully</response>
        /// <response code="400">Bad Request - invalid input</response>
        /// <response code="401">Unauthorized - missing or invalid JWT token</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateAlert([FromBody] CreateAlertDto dto)
        {
            try
            {
                var alert = await _alertService.CreateAlertAsync(UserId, dto);
                return CreatedAtAction(nameof(GetAlertById), new { id = alert.Id }, alert);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing price alert.
        /// </summary>
        /// <param name="id">The alert ID (GUID)</param>
        /// <param name="dto">Alert update data (TargetPrice and/or Direction)</param>
        /// <returns>The updated alert</returns>
        /// <response code="200">Alert updated successfully</response>
        /// <response code="400">Bad Request - invalid input or triggered alert</response>
        /// <response code="401">Unauthorized - missing or invalid JWT token</response>
        /// <response code="403">Forbidden - alert belongs to a different user</response>
        /// <response code="404">Not Found - alert does not exist</response>
        /// <response code="409">Conflict - cannot update a triggered alert</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateAlert(Guid id, [FromBody] UpdateAlertDto dto)
        {
            try
            {
                var alert = await _alertService.UpdateAlertAsync(UserId, id, dto);
                return Ok(alert);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Alert not found" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete an alert.
        /// </summary>
        /// <param name="id">The alert ID (GUID)</param>
        /// <returns>No content</returns>
        /// <response code="204">Alert deleted successfully</response>
        /// <response code="401">Unauthorized - missing or invalid JWT token</response>
        /// <response code="403">Forbidden - alert belongs to a different user</response>
        /// <response code="404">Not Found - alert does not exist</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAlert(Guid id)
        {
            try
            {
                await _alertService.DeleteAlertAsync(UserId, id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Alert not found" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
