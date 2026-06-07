using System.Net.Http.Json;
using System.Text.Json;
using Investe.Application.Interfaces.Services;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPortfolioService _portfolioService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatService> _logger;
        private readonly ICoinPriceService _coinPriceService;

        public ChatService(
            IUnitOfWork unitOfWork,
            IPortfolioService portfolioService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ChatService> logger,
            ICoinPriceService coinPriceService)
        {
            _unitOfWork = unitOfWork;
            _portfolioService = portfolioService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _coinPriceService = coinPriceService;
        }

        private readonly string[] _modelEndpoints = new[] 
        { 
            "models/gemini-2.0-flash:generateContent",
            "models/gemini-2.5-flash:generateContent"
        };

        public async Task<string> AskQuestionAsync(Guid userId, string question)
        {
            var userMsg = new ChatMessage { UserId = userId, Role = "user", Content = question };
            await _unitOfWork.ChatMessages.AddAsync(userMsg);
            await _unitOfWork.CompleteAsync();

            try
            {
                var portfolio = await _portfolioService.GetSummaryAsync(userId);
                var btcPrice = await _coinPriceService.GetCurrentPriceAsync("BTC");
                
                string context = $"PORTFEL: {portfolio.TotalValue:N2} USD. BTC: {btcPrice:N2} USD.";
                var response = await CallAiWithShufflerAsync(userId, question, context);

                var botMsg = new ChatMessage { UserId = userId, Role = "assistant", Content = response };
                await _unitOfWork.ChatMessages.AddAsync(botMsg);
                await _unitOfWork.CompleteAsync();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatService AI Error");
                return "Asystent AI jest chwilowo niedostępny, ale Twoje dane są bezpieczne.";
            }
        }

        private async Task<string> CallAiWithShufflerAsync(Guid userId, string question, string context)
        {
            var history = await _unitOfWork.ChatMessages.GetByUserIdAsync(userId, 6);
            var contents = new List<object>();
            
            contents.Add(new { role = "user", parts = new[] { new { text = $"System info: {context}. Pisz krótko i po polsku." } } });
            contents.Add(new { role = "model", parts = new[] { new { text = "Przyjąłem." } } });

            foreach (var msg in history.Where(m => m.Content != question))
                contents.Add(new { role = msg.Role == "user" ? "user" : "model", parts = new[] { new { text = msg.Content } } });

            contents.Add(new { role = "user", parts = new[] { new { text = question } } });

            var client = _httpClientFactory.CreateClient("Gemini");
            var apiKey = _configuration["Gemini:ApiKey"];

            foreach (var endpoint in _modelEndpoints)
            {
                try 
                {
                    var response = await client.PostAsJsonAsync($"{endpoint}?key={apiKey}", new { contents = contents });
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                        return json.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                    }
                }
                catch { }
            }
            return "Wybacz, nie mogę teraz połączyć się z serwerem AI.";
        }

        public async Task<IEnumerable<ChatMessage>> GetHistoryAsync(Guid userId)
        {
            return await _unitOfWork.ChatMessages.GetByUserIdAsync(userId);
        }

        public async Task ClearHistoryAsync(Guid userId)
        {
            await _unitOfWork.ChatMessages.ClearByUserIdAsync(userId);
            await _unitOfWork.CompleteAsync();
        }
    }
}
