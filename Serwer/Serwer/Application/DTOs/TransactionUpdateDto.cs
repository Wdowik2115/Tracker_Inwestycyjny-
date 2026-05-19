using System.ComponentModel.DataAnnotations;

namespace Investe.Application.DTOs
{
    public class TransactionUpdateDto
    {
        [Range(0.00000001, double.MaxValue)]
        public decimal? Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PriceAtTime { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Fee { get; set; }

        [StringLength(10)]
        public string? FeeCurrency { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CostBasisPerUnit { get; set; }

        public DateOnly? ExecutedAt { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
