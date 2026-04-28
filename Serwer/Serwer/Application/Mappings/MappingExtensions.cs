using Investe.Application.DTOs;
using Investe.Domain.Entities;

namespace Investe.Application.Mappings
{
    public static class MappingExtensions
    {
        public static WalletDto ToDto(this Wallet wallet)
        {
            return new WalletDto
            {
                Id = wallet.Id,
                Name = wallet.Name,
                Description = wallet.Description
            };
        }

        public static AssetResponseDto ToDto(this Asset asset, decimal currentPrice)
        {
            return new AssetResponseDto
            {
                Id = asset.Id,
                CoinId = asset.CoinId,
                Symbol = asset.Symbol,
                Name = asset.Name,
                Quantity = asset.Quantity,
                AverageBuyPrice = asset.AverageBuyPrice,
                CurrentPrice = currentPrice
            };
        }
    }
}
