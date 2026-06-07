using Investe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Investe.Infrastructure.Persistence.Repositories.Implementations
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public ChatMessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
        }

        public async Task<IEnumerable<ChatMessage>> GetByUserIdAsync(Guid userId, int limit = 50)
        {
            return await _context.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task ClearByUserIdAsync(Guid userId)
        {
            var messages = await _context.ChatMessages.Where(m => m.UserId == userId).ToListAsync();
            _context.ChatMessages.RemoveRange(messages);
        }
    }
}
