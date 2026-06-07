using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Application.Services;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace Serwer.Tests.Application.Services
{
    public class ChatServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IChatMessageRepository> _chatRepoMock;
        private readonly Mock<IPortfolioService> _portfolioServiceMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly ChatService _sut;

        public ChatServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _chatRepoMock = new Mock<IChatMessageRepository>();
            _portfolioServiceMock = new Mock<IPortfolioService>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationMock = new Mock<IConfiguration>();

            _uowMock.Setup(u => u.ChatMessages).Returns(_chatRepoMock.Object);

            _sut = new ChatService(
                _uowMock.Object,
                _portfolioServiceMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object);
        }

        [Fact]
        public async Task AskQuestionAsync_SavesMessagesAndReturnsBotResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var question = "How much BTC do I have?";
            var botResponse = "You have 2 BTC.";

            _portfolioServiceMock.Setup(s => s.GetSummaryAsync(userId))
                .ReturnsAsync(new PortfolioSummaryDto
                {
                    TotalValue = 100000,
                    Positions = new List<PositionDto>
                    {
                        new() { Name = "Bitcoin", Symbol = "BTC", Quantity = 2, Value = 80000 }
                    }
                });

            _chatRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<int>()))
                .ReturnsAsync(new List<ChatMessage>());

            _configurationMock.Setup(c => c["Gemini:ApiKey"]).Returns("test-key");

            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new
                {
                    candidates = new[]
                    {
                        new
                        {
                            content = new
                            {
                                parts = new[] { new { text = botResponse } }
                            }
                        }
                    }
                })
            };

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://api.gemini.com/")
            };

            _httpClientFactoryMock.Setup(f => f.CreateClient("Gemini")).Returns(httpClient);

            // Act
            var result = await _sut.AskQuestionAsync(userId, question);

            // Assert
            Assert.Equal(botResponse, result);
            _chatRepoMock.Verify(r => r.AddAsync(It.Is<ChatMessage>(m => m.Content == question && m.Role == "user")), Times.Once);
            _chatRepoMock.Verify(r => r.AddAsync(It.Is<ChatMessage>(m => m.Content == botResponse && m.Role == "assistant")), Times.Once);
            _uowMock.Verify(u => u.CompleteAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task GetHistoryAsync_ReturnsMessagesFromRepo()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var history = new List<ChatMessage>
            {
                new() { Content = "Hi", Role = "user" },
                new() { Content = "Hello", Role = "assistant" }
            };

            _chatRepoMock.Setup(r => r.GetByUserIdAsync(userId, 50))
                .ReturnsAsync(history);

            // Act
            var result = await _sut.GetHistoryAsync(userId);

            // Assert
            Assert.Equal(history, result);
        }
    }
}
