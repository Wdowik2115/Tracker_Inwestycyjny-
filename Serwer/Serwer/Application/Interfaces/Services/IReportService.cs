using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface IReportService
    {
        Task<ReportDto> GenerateAccountReportAsync(Guid userId);
        Task<ReportDto> GenerateWalletReportAsync(Guid userId, Guid walletId);
        Task<IEnumerable<ReportDto>> GetReportsAsync(Guid userId);
        Task<(byte[] Content, string FileName)> GetReportFileAsync(Guid userId, Guid reportId);
        Task DeleteReportAsync(Guid userId, Guid reportId);
    }
}
