using Investe.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serwer.Extensions;

namespace Serwer.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _reportService.GetReportsAsync(User.GetUserId());
            return Ok(reports);
        }

        [HttpPost("account")]
        public async Task<IActionResult> GenerateAccountReport()
        {
            var report = await _reportService.GenerateAccountReportAsync(User.GetUserId());
            return Ok(report);
        }

        [HttpPost("wallet/{walletId:guid}")]
        public async Task<IActionResult> GenerateWalletReport(Guid walletId)
        {
            var report = await _reportService.GenerateWalletReportAsync(User.GetUserId(), walletId);
            return Ok(report);
        }

        [HttpGet("{id:guid}/download")]
        public async Task<IActionResult> DownloadReport(Guid id)
        {
            var (fileBytes, fileName) = await _reportService.GetReportFileAsync(User.GetUserId(), id);
            return File(fileBytes, "application/pdf", fileName);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteReport(Guid id)
        {
            await _reportService.DeleteReportAsync(User.GetUserId(), id);
            return NoContent();
        }
    }
}
