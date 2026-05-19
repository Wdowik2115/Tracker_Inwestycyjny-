using System.ComponentModel.DataAnnotations;
using Investe.Domain.Entities;

namespace Investe.Application.DTOs
{
    public class CreateAlertDto
    {
        [Required]
        [StringLength(20)]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        [Range(0.0001, double.MaxValue)]
        public decimal TargetPrice { get; set; }

        [Required]
        public AlertDirection Direction { get; set; }
    }
}
