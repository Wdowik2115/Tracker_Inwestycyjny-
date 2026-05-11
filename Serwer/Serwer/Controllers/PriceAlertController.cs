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

        [HttpGet]
        public async Task<IActionResult> GetAlerts()
        {
            var alerts = await _alertService.GetUserAlertsAsync(User.GetUserId());
            return Ok(alerts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAlert([FromBody] CreateAlertDto dto)
        {
            var alert = await _alertService.CreateAlertAsync(User.GetUserId(), dto);
            return CreatedAtAction(nameof(GetAlerts), new { }, alert);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAlert(Guid id)
        {
            await _alertService.DeleteAlertAsync(User.GetUserId(), id);
            return NoContent();
        }
    }
}
