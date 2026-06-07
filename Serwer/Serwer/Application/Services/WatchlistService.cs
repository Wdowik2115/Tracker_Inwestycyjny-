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

            return items.Select(i => MapToDto(i, prices.GetValueOrDefault(i.Symbol, 0)));
        }

        public async Task<WatchlistItemDto> GetWatchlistItemByIdAsync(Guid userId, Guid id)
        {
            var item = await _unitOfWork.Watchlist.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Watchlist item {id} not found.");

            if (item.UserId != userId)
                throw new UnauthorizedAccessException("Watchlist item does not belong to this user.");

            var price = await _priceService.GetCurrentPriceAsync(item.Symbol);
            return MapToDto(item, price);
        }

        public async Task<(WatchlistItemDto Item, bool IsCreated)> AddToWatchlistAsync(Guid userId, AddToWatchlistDto dto)
        {
            var existing = await _unitOfWork.Watchlist.GetByUserAndCoinAsync(userId, dto.CoinId);
            if (existing != null)
                return (MapToDto(existing, await _priceService.GetCurrentPriceAsync(dto.Symbol)), false);

            var item = new WatchlistItem
            {
                UserId = userId,
                CoinId = dto.CoinId,
                Symbol = dto.Symbol,
                ImageUrl = dto.ImageUrl
            };

            await _unitOfWork.Watchlist.AddAsync(item);
            await _unitOfWork.CompleteAsync();

            return (MapToDto(item, await _priceService.GetCurrentPriceAsync(dto.Symbol)), true);
        }

        public async Task RemoveFromWatchlistAsync(Guid userId, Guid id)
        {
            var item = await _unitOfWork.Watchlist.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Watchlist item {id} not found.");

            if (item.UserId != userId)
                throw new UnauthorizedAccessException("Watchlist item does not belong to this user.");

            await _unitOfWork.Watchlist.DeleteAsync(item);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<bool> IsOnWatchlistAsync(Guid userId, string coinId)
        {
            var item = await _unitOfWork.Watchlist.GetByUserAndCoinAsync(userId, coinId);
            return item != null;
        }

        private static WatchlistItemDto MapToDto(WatchlistItem item, decimal currentPrice)
        {
            return new WatchlistItemDto
            {
                Id = item.Id,
                CoinId = item.CoinId,
                Symbol = item.Symbol,
                ImageUrl = item.ImageUrl,
                AddedAt = item.AddedAt,
                CurrentPrice = currentPrice
            };
        }
    }
}
