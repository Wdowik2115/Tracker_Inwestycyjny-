using Microsoft.EntityFrameworkCore;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories.Implementations
{
    public class ReportRepository : BaseRepository<Report>, IReportRepository
    {
        public ReportRepository(ApplicationDbContext dbContext) : base(dbContext) { }

        public async Task<IEnumerable<Report>> GetReportsByUserIdAsync(Guid userId)
        {
            // Select without Content to avoid loading large blobs when listing
            return await _dbContext.Reports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.GeneratedAt)
                .Select(r => new Report
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    WalletId = r.WalletId,
                    Wallet = r.Wallet,
                    Type = r.Type,
                    Title = r.Title,
                    FileName = r.FileName,
                    FileSizeBytes = r.FileSizeBytes,
                    GeneratedAt = r.GeneratedAt,
                    Content = Array.Empty<byte>()
                })
                .ToListAsync();
        }

        public async Task<Report?> GetReportByIdAndUserIdAsync(Guid reportId, Guid userId)
        {
            return await _dbContext.Reports
                .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId);
        }
    }
}
