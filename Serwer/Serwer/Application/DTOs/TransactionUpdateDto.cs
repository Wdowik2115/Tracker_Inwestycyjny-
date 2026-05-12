using System.ComponentModel.DataAnnotations;

namespace Investe.Application.DTOs
{
    public class TransactionUpdateDto
    {
        [Range(0, double.MaxValue)]
        public decimal? PriceAtTime { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CostBasisPerUnit { get; set; }

        public DateTime? ExecutedAt { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
