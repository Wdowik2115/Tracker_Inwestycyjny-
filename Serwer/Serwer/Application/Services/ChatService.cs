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
        private readonly IGeminiApiService _geminiApiService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatService> _logger;
        private readonly ICoinPriceService _coinPriceService;

        public ChatService(
            IUnitOfWork unitOfWork,
            IPortfolioService portfolioService,
            IGeminiApiService geminiApiService,
            IConfiguration configuration,
            ILogger<ChatService> logger,
            ICoinPriceService coinPriceService)
        {
            _unitOfWork = unitOfWork;
            _portfolioService = portfolioService;
            _geminiApiService = geminiApiService;
            _configuration = configuration;
            _logger = logger;
            _coinPriceService = coinPriceService;
        }

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
                
                // Fetch top movers to give AI some global context too
                var topMovers = await _coinPriceService.GetTopMoversAsync(3);
                var moversInfo = string.Join(", ", topMovers.Select(m => $"{m.Symbol} (+{m.PriceChangePercentage24h:N2}%)"));

                string context = $"CAŁY PORTFEL UŻYTKOWNIKA: {allAssetsInfo}. ŁĄCZNA WARTOŚĆ: {portfolio.TotalValue:N2} USD. ŁĄCZNY ZYSK/STRATA: {portfolio.TotalPnl:N2} USD. Cena BTC: {btcPrice:N2} USD. TOP WZROSTY RYNKOWE: {moversInfo}.";
                
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

        private string FormatDecimal(decimal value, int precision = 2)
        {
            // Remove trailing zeros and format with thousands separator
            string formatted = value.ToString($"N{precision}");
            if (formatted.Contains("."))
            {
                formatted = formatted.TrimEnd('0').TrimEnd('.');
            }
            return formatted;
        }

        private async Task<string?> TryGetLocalResponseAsync(Guid userId, string question, PortfolioSummaryDto portfolio)
        {
            var q = question.ToLower();
            
            // 1. Context detection
            bool hasPersonalContext = q.Contains("moje") || q.Contains("mój") || q.Contains("moj") || q.Contains("mam") || q.Contains("posiadam") || q.Contains("moich") || q.Contains("portfel");
            bool isGlobalMarketQuery = q.Contains("kryptowalut") || q.Contains("rynek") || q.Contains("rynku") || q.Contains("giełd") || q.Contains("gield") || q.Contains("na świecie") || q.Contains("wszystkie");

            // 2. Global Market Trends / Top Movers
            try 
            {
                if (isGlobalMarketQuery || q.Contains("wzrost") || q.Contains("wzorst") || q.Contains("rosnie") || q.Contains("rośnie") || q.Contains("zyska") || q.Contains("zliacz") ||
                    q.Contains("spadek") || q.Contains("traci") || q.Contains("najlepiej") || q.Contains("najgorzej") || q.Contains("trend") || q.Contains("top") || 
                    (q.Contains("zysk") && !hasPersonalContext))
                {
                    bool isLookingForLosers = q.Contains("spadek") || q.Contains("najgorzej") || q.Contains("traci") || q.Contains("spada");
                    var movers = await _coinPriceService.GetTopMoversAsync(5, isLookingForLosers);
                    if (movers.Any())
                    {
                        var title = isLookingForLosers ? "Największe spadki (24h):" : "Największe wzrosty (24h):";
                        var list = string.Join("\n", movers.Select(m => $"- {m.Name} ({m.Symbol}): {FormatDecimal(m.CurrentPrice)} USD ({FormatDecimal(m.PriceChangePercentage24h)}%)"));
                        return $"{title}\n{list}";
                    }
                    else if (isGlobalMarketQuery)
                    {
                        return "Obecnie mam trudności z pobraniem najświeższych danych rynkowych z CoinGecko. Spróbuj zapytać o konkretną cenę, np. 'cena BTC'.";
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error in Top Movers local intent"); }

            // 3. Portfolio summary & Specific Assets
            try 
            {
                if (q.Contains("portfel") || (hasPersonalContext && (q.Contains("aktywa") || q.Contains("co tam") || q.Contains("stan"))))
                {
                    var words = q.Split(new[] { ' ', '?', '!', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        var symbol = word.ToUpper();
                        var position = portfolio.Positions.FirstOrDefault(p => p.Symbol == symbol);
                        if (position != null)
                        {
                            return $"Masz {FormatDecimal(position.Quantity, 6)} {position.Symbol} o wartości {FormatDecimal(position.Value)} USD. Twój zysk/strata na tym aktywie to {FormatDecimal(position.Pnl)} USD.";
                        }
                    }

                    if (!portfolio.Positions.Any()) return "Twój portfel jest obecnie pusty. Dodaj transakcje, aby zacząć śledzenie!";
                    var assetList = string.Join(", ", portfolio.Positions.Select(p => $"{FormatDecimal(p.Quantity, 6)} {p.Symbol}"));
                    return $"W Twoim portfelu masz: {assetList}. Łączna wartość: {FormatDecimal(portfolio.TotalValue)} USD.";
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error in Portfolio local intent"); }

            // 4. Personal P&L
            try 
            {
                if (hasPersonalContext && (q.Contains("zysk") || q.Contains("strata") || q.Contains("p&l") || q.Contains("wynik") || q.Contains("zarobiłem") || q.Contains("zarobilem")))
                {
                    return $"Twój obecny zysk/strata (P&L) to {FormatDecimal(portfolio.TotalPnl)} USD. Całkowita wartość portfela wynosi {FormatDecimal(portfolio.TotalValue)} USD.";
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error in P&L local intent"); }

            // 5. Transactions
            try 
            {
                if (q.Contains("transakcj") || q.Contains("histori") || (hasPersonalContext && (q.Contains("kupiłem") || q.Contains("kupilem") || q.Contains("sprzedałem") || q.Contains("sprzedalem"))))
                {
                    var transactions = await _unitOfWork.Transactions.GetByUserIdAsync(userId, 3);
                    if (!transactions.Any()) return "Nie masz jeszcze żadnych zarejestrowanych transakcji.";
                    
                    var list = string.Join("\n", transactions.Select(t => $"- {t.Type} {FormatDecimal(t.Quantity, 6)} {t.Symbol} po {FormatDecimal(t.PriceAtTime)} USD ({t.ExecutedAt:dd.MM.yyyy})"));
                    return $"Twoje ostatnie transakcje to:\n{list}";
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error in Transactions local intent"); }

            // 6. Alerts
            try 
            {
                if (q.Contains("alert") || q.Contains("powiadomienia"))
                {
                    var alerts = await _unitOfWork.PriceAlerts.GetByUserIdAsync(userId);
                    var activeAlerts = alerts.Where(a => !a.IsTriggered).ToList();
                    
                    // Check for specific coin in question
                    var words = q.Split(new[] { ' ', '?', '!', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        var symbol = word.ToUpper();
                        var coinAlerts = activeAlerts.Where(a => a.Symbol == symbol).ToList();
                        if (coinAlerts.Any())
                        {
                            var coinList = string.Join("\n", coinAlerts.Select(a => $"- {a.Symbol} {(a.Direction == Investe.Domain.Entities.AlertDirection.Above ? ">" : "<")} {FormatDecimal(a.TargetPrice)} USD"));
                            return $"Twoje aktywne alerty dla {symbol}:\n{coinList}";
                        }
                    }

                    if (!activeAlerts.Any()) return "Nie masz obecnie żadnych aktywnych alertów cenowych.";
                    
                    var list = string.Join("\n", activeAlerts.Select(a => $"- {a.Symbol} {(a.Direction == Investe.Domain.Entities.AlertDirection.Above ? ">" : "<")} {FormatDecimal(a.TargetPrice)} USD"));
                    return $"Twoje aktywne alerty:\n{list}";
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error in Alerts local intent"); }

            // 7. Watchlist
            try
            {
                if (q.Contains("obserwowan") || q.Contains("watchlist") || q.Contains("ulubione") || q.Contains("śledzę") || q.Contains("sledze"))
                {
                    var watchlist = await _unitOfWork.Watchlist.GetByUserIdAsync(userId);
                    if (!watchlist.Any()) return "Twoja lista obserwowanych jest obecnie pusta. Dodaj monety w sekcji Rynku, aby je tu zobaczyć!";
                    
                    var list = string.Join(", ", watchlist.Select(w => w.Symbol.ToUpper()));
                    return $"Twoja lista obserwowanych kryptowalut: {list}.";
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error in Watchlist local intent"); }

            // 8. Price keywords
            try 
            {
                if (q.Contains("cena") || q.Contains("kurs") || q.Contains("ile kosztuje"))
                {
                    var words = q.Split(new[] { ' ', '?', '!', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        var symbol = word.ToUpper();
                        if (symbol.Length >= 2 && symbol.Length <= 5)
                        {
                            var price = await _coinPriceService.GetCurrentPriceAsync(symbol);
                            if (price > 0) return $"Aktualna cena {symbol} to {FormatDecimal(price)} USD.";
                        }
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Error in Price local intent"); }

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
            var historyFromDb = (await _unitOfWork.ChatMessages.GetByUserIdAsync(userId, 10)).ToList();
            var contents = new List<object>();
            
            // 1. Initial System Instruction as a user message
            contents.Add(new { role = "user", parts = new[] { new { text = $"CONTEXT: {context}. Jesteś ekspertem Investee. Odpowiadaj krótko i po polsku." } } });
            contents.Add(new { role = "model", parts = new[] { new { text = "Przyjąłem. Jak mogę pomóc?" } } });

            // 2. Add history with strict alternation check
            string lastRole = "model";
            foreach (var msg in historyFromDb)
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

            try 
            {
                return await _geminiApiService.GenerateContentAsync(question, contents);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API failed, falling back to emergency mode message.");
                return "W tej chwili mam trudności z połączeniem się z moimi modułami analitycznymi. Mogę jednak odpowiedzieć na pytania o ceny i Twój portfel, ponieważ te dane przetwarzam lokalnie.";
            }
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
