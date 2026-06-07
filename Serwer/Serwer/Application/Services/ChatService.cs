using System.Net.Http.Json;
using System.Text.Json;
using Investe.Application.Interfaces.Services;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Investe.Application.DTOs;

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
            try
            {
                var portfolio = await _portfolioService.GetSummaryAsync(userId);
                
                // 1. Local NLP / Fast Match - Intercept simple queries
                var localResponse = await TryGetLocalResponseAsync(userId, question, portfolio);
                if (localResponse != null)
                {
                    await SaveChatStepAsync(userId, question, localResponse);
                    return localResponse;
                }

                // 2. AI Hybrid Logic - Add rich context for the LLM
                var btcPrice = await _coinPriceService.GetCurrentPriceAsync("BTC");
                var allAssetsInfo = string.Join(", ", portfolio.Positions
                    .Select(p => $"{p.Symbol} ({p.Quantity:N4} szt, wart. {p.Value:N2} USD, P&L: {p.Pnl:N2} USD)"));
                
                string context = $"CAŁY PORTFEL: {allAssetsInfo}. ŁĄCZNA WARTOŚĆ: {portfolio.TotalValue:N2} USD. ŁĄCZNY ZYSK/STRATA: {portfolio.TotalPnl:N2} USD. Cena BTC: {btcPrice:N2} USD.";
                
                var response = await CallAiWithShufflerAsync(userId, question, context);

                await SaveChatStepAsync(userId, question, response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatService Error");
                return "Asystent AI jest chwilowo niedostępny. Możesz jednak zapytać o ceny lub swój portfel - te funkcje działają lokalnie.";
            }
        }

        private async Task<string?> TryGetLocalResponseAsync(Guid userId, string question, PortfolioSummaryDto portfolio)
        {
            var q = question.ToLower();
            
            // Portfolio summary keywords
            if (q.Contains("portfel") || q.Contains("moje aktywa") || q.Contains("co mam") || q.Contains("posiadam"))
            {
                if (!portfolio.Positions.Any()) return "Twój portfel jest obecnie pusty. Dodaj transakcje, aby zacząć śledzenie!";
                var assetList = string.Join(", ", portfolio.Positions.Select(p => $"{p.Quantity:N4} {p.Symbol}"));
                return $"W Twoim portfelu masz: {assetList}. Łączna wartość: {portfolio.TotalValue:N2} USD.";
            }

            // P&L keywords
            if (q.Contains("zysk") || q.Contains("strata") || q.Contains("p&l") || q.Contains("wynik") || q.Contains("zarobiłem"))
            {
                return $"Twój obecny zysk/strata (P&L) to {portfolio.TotalPnl:N2} USD. Całkowita wartość portfela wynosi {portfolio.TotalValue:N2} USD.";
            }

            // Price keywords
            if (q.Contains("cena") || q.Contains("kurs") || q.Contains("ile kosztuje"))
            {
                var words = q.Split(new[] { ' ', '?', '!', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var symbol = word.ToUpper();
                    if (symbol.Length >= 2 && symbol.Length <= 5)
                    {
                        var price = await _coinPriceService.GetCurrentPriceAsync(symbol);
                        if (price > 0) return $"Aktualna cena {symbol} to {price:N2} USD.";
                    }
                }
            }

            return null;
        }

        private async Task SaveChatStepAsync(Guid userId, string question, string response)
        {
            // Save both user question and bot response to history
            await _unitOfWork.ChatMessages.AddAsync(new ChatMessage { UserId = userId, Role = "user", Content = question });
            await _unitOfWork.ChatMessages.AddAsync(new ChatMessage { UserId = userId, Role = "assistant", Content = response });
            await _unitOfWork.CompleteAsync();
        }

        private async Task<string> CallAiWithShufflerAsync(Guid userId, string question, string context)
        {
            // Fetch history BEFORE adding current question to DB to avoid double entry
            var history = (await _unitOfWork.ChatMessages.GetByUserIdAsync(userId, 10)).ToList();
            var contents = new List<object>();
            
            // 1. Initial System Instruction as a user message
            contents.Add(new { role = "user", parts = new[] { new { text = $"CONTEXT: {context}. Jesteś ekspertem Investee. Odpowiadaj krótko i po polsku." } } });
            contents.Add(new { role = "model", parts = new[] { new { text = "Przyjąłem. Jak mogę pomóc?" } } });

            // 2. Add history with strict alternation check
            string lastRole = "model";
            foreach (var msg in history)
            {
                string currentRole = msg.Role == "user" ? "user" : "model";
                
                // Gemini fails on consecutive same roles. Skip if role is same as last.
                if (currentRole != lastRole)
                {
                    contents.Add(new { role = currentRole, parts = new[] { new { text = msg.Content } } });
                    lastRole = currentRole;
                }
            }

            // 3. Add current question (must be 'user' after 'model')
            if (lastRole == "user")
            {
                // If last was user, we need a dummy model response to alternate
                contents.Add(new { role = "model", parts = new[] { new { text = "Rozumiem, kontynuuj." } } });
            }
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
                        return json.GetProperty("candidates")[0]
                                   .GetProperty("content")
                                   .GetProperty("parts")[0]
                                   .GetProperty("text")
                                   .GetString() ?? "Błąd parsowania.";
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Gemini API Error ({endpoint}): {response.StatusCode} - {error}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception calling {endpoint}");
                }
            }
            return "Niestety wszystkie modele AI są teraz zajęte. Spróbuj ponownie za chwilę.";
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
