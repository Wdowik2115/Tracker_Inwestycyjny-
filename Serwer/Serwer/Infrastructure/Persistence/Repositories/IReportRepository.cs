using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories
{
    public interface IReportRepository : IBaseRepository<Report>
    {
        Task<IEnumerable<Report>> GetReportsByUserIdAsync(Guid userId);
        Task<Report?> GetReportByIdAndUserIdAsync(Guid reportId, Guid userId);
    }
}
