using System.ComponentModel.DataAnnotations;
using Investe.Domain.Entities;

namespace Investe.Application.DTOs
{
    public class UpdateAlertDto
    {
        [Range(0.0001, double.MaxValue)]
        public decimal? TargetPrice { get; set; }

        public AlertDirection? Direction { get; set; }
    }
}
