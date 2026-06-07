using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serwer.Extensions;

namespace Serwer.Controllers
{
    [ApiController]
    [Route("api/wallets")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet]
        public async Task<IActionResult> GetWallets()
        {
            var wallets = await _walletService.GetUserWalletsAsync(User.GetUserId());
            return Ok(wallets);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetWalletDetails(Guid id)
        {
            var wallet = await _walletService.GetWalletDetailsAsync(User.GetUserId(), id);
            return Ok(wallet);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWallet([FromBody] CreateWalletDto dto)
        {
            var wallet = await _walletService.CreateWalletAsync(User.GetUserId(), dto);
            return CreatedAtAction(nameof(GetWallets), new { }, wallet);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateWallet(Guid id, [FromBody] UpdateWalletDto dto)
        {
            var wallet = await _walletService.UpdateWalletAsync(User.GetUserId(), id, dto);
            return Ok(wallet);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteWallet(Guid id)
        {
            await _walletService.DeleteWalletAsync(User.GetUserId(), id);
            return NoContent();
        }

        [HttpGet("{id:guid}/history")]
        public async Task<IActionResult> GetWalletHistory(Guid id, [FromQuery] int days = 30)
        {
            var history = await _walletService.GetWalletHistoryAsync(User.GetUserId(), id, Math.Clamp(days, 7, 365));
            return Ok(history);
        }

        [HttpPost("{id:guid}/share")]
        public async Task<IActionResult> ShareWallet(Guid id, [FromBody] ShareWalletDto dto)
        {
            await _walletService.ShareWalletAsync(User.GetUserId(), id, dto.Email);
            return Ok();
        }

        [HttpDelete("{id:guid}/share/{email}")]
        public async Task<IActionResult> UnshareWallet(Guid id, string email)
        {
            await _walletService.UnshareWalletAsync(User.GetUserId(), id, email);
            return Ok();
        }
    }
}
