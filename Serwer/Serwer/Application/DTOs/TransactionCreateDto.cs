using System.ComponentModel.DataAnnotations;

namespace Investe.Application.DTOs
{
    public class TransactionCreateDto
    {
        [Required]
        public Guid WalletId { get; set; }

        [Required]
        [StringLength(100)]
        public string CoinId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>Accepted values: "Buy" or "Sell".</summary>
        [Required]
        public string Type { get; set; } = "Buy";

        [Required]
        [Range(0.00000001, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PriceAtTime { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CostBasisPerUnit { get; set; }

        public DateTime? ExecutedAt { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }
    }
}
