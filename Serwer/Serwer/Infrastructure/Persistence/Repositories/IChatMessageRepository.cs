using Investe.Domain.Entities;

namespace Investe.Infrastructure.Persistence.Repositories
{
    public interface IChatMessageRepository
    {
        Task AddAsync(ChatMessage message);
        Task<IEnumerable<ChatMessage>> GetByUserIdAsync(Guid userId, int limit = 50);
        Task ClearByUserIdAsync(Guid userId);
    }
}
