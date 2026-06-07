using System.Text.Json;

namespace Investe.Application.Interfaces.Services
{
    public interface IGeminiApiService
    {
        Task<string> GenerateContentAsync(string prompt, List<object> history);
    }
}
