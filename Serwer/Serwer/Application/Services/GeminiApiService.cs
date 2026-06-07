using System.Net.Http.Json;
using System.Text.Json;
using Investe.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class GeminiApiService : IGeminiApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiApiService> _logger;

        private readonly string[] _modelEndpoints = new[] 
        { 
            "models/gemini-2.0-flash:generateContent",
            "models/gemini-2.5-flash:generateContent"
        };

        public GeminiApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GeminiApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GenerateContentAsync(string prompt, List<object> history)
        {
            var client = _httpClientFactory.CreateClient("Gemini");
            var apiKey = _configuration["Gemini:ApiKey"];

            foreach (var endpoint in _modelEndpoints)
            {
                try 
                {
                    var response = await client.PostAsJsonAsync($"{endpoint}?key={apiKey}", new { contents = history });
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                        return json.GetProperty("candidates")[0]
                                   .GetProperty("content")
                                   .GetProperty("parts")[0]
                                   .GetProperty("text")
                                   .GetString() ?? "Błąd parsowania odpowiedzi AI.";
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Gemini API Warning ({endpoint}): {response.StatusCode} - {error}");
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            // Could implement a delay or specific fallback here
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception calling Gemini endpoint: {endpoint}");
                }
            }

            throw new Exception("Wszystkie modele Gemini są obecnie nieosiągalne lub przekroczono limity.");
        }
    }
}
