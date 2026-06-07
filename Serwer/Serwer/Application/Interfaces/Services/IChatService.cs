using Investe.Domain.Entities;

namespace Investe.Application.Interfaces.Services
{
    public interface IChatService
    {
        Task<string> AskQuestionAsync(Guid userId, string question);
        Task<IEnumerable<ChatMessage>> GetHistoryAsync(Guid userId);
        Task ClearHistoryAsync(Guid userId);
    }
}
