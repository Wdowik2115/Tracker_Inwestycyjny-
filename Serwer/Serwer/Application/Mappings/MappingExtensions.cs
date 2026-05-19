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

        public static TransactionDto ToDto(this Transaction t)
        {
            return new TransactionDto
            {
                Id = t.Id,
                WalletId = t.WalletId,
                WalletName = t.Wallet?.Name ?? "Unknown",
                CoinId = t.CoinId,
                Symbol = t.Symbol,
                Type = t.Type.ToString(),
                Quantity = t.Quantity,
                PriceAtTime = t.PriceAtTime,
                TotalValue = t.TotalValue,
                Fee = t.Fee,
                FeeCurrency = t.FeeCurrency,
                CostBasisPerUnit = t.CostBasisPerUnit,
                CostBasisSource = t.CostBasisSource,
                ExecutedAt = DateOnly.FromDateTime(t.ExecutedAt),
                Notes = t.Notes
            };
        }

        public static AlertDto ToDto(this PriceAlert a)
        {
            return new AlertDto
            {
                Id = a.Id,
                Symbol = a.Symbol,
                TargetPrice = a.TargetPrice,
                Direction = a.Direction,
                IsTriggered = a.IsTriggered,
                TriggeredAt = a.TriggeredAt,
                CreatedAt = a.CreatedAt
            };
        }
    }
}
