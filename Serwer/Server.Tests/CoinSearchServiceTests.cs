using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Investe.Tests
{
    public class CoinSearchServiceTests
    {
        private readonly Mock<ICoinPriceService> _mockCoinPriceService;
        private readonly Mock<ILogger<CoinSearchService>> _mockLogger;
        private readonly CoinSearchService _service;

        public CoinSearchServiceTests()
        {
            _mockCoinPriceService = new Mock<ICoinPriceService>();
            _mockLogger = new Mock<ILogger<CoinSearchService>>();
            _service = new CoinSearchService(_mockCoinPriceService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SearchCoinsAsync_WithValidQuery_ReturnsCoinSearchDtos()
        {
            // Arrange
            var query = "BTC";
            _mockCoinPriceService
                .Setup(x => x.GetCoinImageUrlAsync(It.IsAny<string>()))
                .ReturnsAsync("https://example.com/btc.png");

            // Act
            var result = await _service.SearchCoinsAsync(query);

            // Assert
            Assert.NotEmpty(result);
            Assert.Single(result);
            Assert.Equal("BTC", result[0].Symbol);
            Assert.Equal("Bitcoin", result[0].Name);
            Assert.Equal("bitcoin", result[0].CoinId);
            Assert.Equal("https://example.com/btc.png", result[0].ImageUrl);
        }

        [Fact]
        public async Task SearchCoinsAsync_WithPartialMatch_ReturnsCoinSearchDtos()
        {
            // Arrange
            var query = "ET";
            _mockCoinPriceService
                .Setup(x => x.GetCoinImageUrlAsync(It.IsAny<string>()))
                .ReturnsAsync("https://example.com/eth.png");

            // Act
            var result = await _service.SearchCoinsAsync(query);

            // Assert
            Assert.Single(result);
            Assert.Equal("ETH", result[0].Symbol);
        }

        [Fact]
        public async Task SearchCoinsAsync_WithEmptyQuery_ReturnsEmptyList()
        {
            // Act
            var result = await _service.SearchCoinsAsync(string.Empty);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchCoinsAsync_WithNoMatches_ReturnsEmptyList()
        {
            // Act
            var result = await _service.SearchCoinsAsync("ZZZZZ");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCoinDetailsAsync_WithValidCoinId_ReturnsCoinDetailDto()
        {
            // Arrange
            var coinId = "bitcoin";
            _mockCoinPriceService
                .Setup(x => x.GetCoinImageUrlAsync(coinId))
                .ReturnsAsync("https://example.com/btc.png");

            // Act
            var result = await _service.GetCoinDetailsAsync(coinId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("bitcoin", result.CoinId);
            Assert.Equal("BTC", result.Symbol);
            Assert.Equal("Bitcoin", result.Name);
            Assert.Equal("https://example.com/btc.png", result.ImageUrl);
        }

        [Fact]
        public async Task GetCoinDetailsAsync_WithInvalidCoinId_ReturnsNull()
        {
            // Act
            var result = await _service.GetCoinDetailsAsync("invalid-coin-123");

            // Assert
            Assert.Null(result);
        }
    }
}
