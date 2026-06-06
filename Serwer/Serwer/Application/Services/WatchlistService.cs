using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;

namespace Investe.Application.Services
{
    public class WatchlistService : IWatchlistService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoinPriceService _priceService;

        public WatchlistService(IUnitOfWork unitOfWork, ICoinPriceService priceService)
        {
            _unitOfWork = unitOfWork;
            _priceService = priceService;
        }

        public async Task<IEnumerable<WatchlistItemDto>> GetWatchlistAsync(Guid userId)
        {
            var items = await _unitOfWork.Watchlist.GetByUserIdAsync(userId);
            var symbols = items.Select(i => i.Symbol).Distinct();
            var prices = await _priceService.GetCurrentPricesAsync(symbols);

            return items.Select(i => new WatchlistItemDto
            {
                Id = i.Id,
                CoinId = i.CoinId,
                Symbol = i.Symbol,
                AddedAt = i.AddedAt,
                CurrentPrice = prices.GetValueOrDefault(i.Symbol, 0)
            });
        }

        public async Task<WatchlistItemDto> AddToWatchlistAsync(Guid userId, AddToWatchlistDto dto)
        {
            var existing = await _unitOfWork.Watchlist.GetByUserAndCoinAsync(userId, dto.CoinId);
            if (existing != null)
                return MapToDto(existing, await _priceService.GetCurrentPriceAsync(dto.Symbol));

            var item = new WatchlistItem
            {
                UserId = userId,
                CoinId = dto.CoinId,
                Symbol = dto.Symbol
            };

            await _unitOfWork.Watchlist.AddAsync(item);
            await _unitOfWork.CompleteAsync();

            return MapToDto(item, await _priceService.GetCurrentPriceAsync(dto.Symbol));
        }

        public async Task RemoveFromWatchlistAsync(Guid userId, Guid id)
        {
            var item = await _unitOfWork.Watchlist.GetByIdAsync(id);
            if (item != null && item.UserId == userId)
            {
                await _unitOfWork.Watchlist.DeleteAsync(item);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task<bool> IsOnWatchlistAsync(Guid userId, string coinId)
        {
            var item = await _unitOfWork.Watchlist.GetByUserAndCoinAsync(userId, coinId);
            return item != null;
        }

        public async Task<IEnumerable<string>> GetSuggestionsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Enumerable.Empty<string>();

            var supportedCoins = await _priceService.GetSupportedCoinsAsync();
            var q = query.ToUpperInvariant();

            return supportedCoins.Keys
                .Where(s => s.StartsWith(q))
                .OrderBy(s => s)
                .Take(5);
        }

        private static WatchlistItemDto MapToDto(WatchlistItem item, decimal currentPrice)
        {
            return new WatchlistItemDto
            {
                Id = item.Id,
                CoinId = item.CoinId,
                Symbol = item.Symbol,
                AddedAt = item.AddedAt,
                CurrentPrice = currentPrice
            };
        }
    }
}
