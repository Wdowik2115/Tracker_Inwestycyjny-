using Investe.Domain.Entities;

namespace Investe.Application.DTOs
{
    public class AlertDto
    {
        public Guid Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public decimal TargetPrice { get; set; }
        public AlertDirection Direction { get; set; }
        public bool IsTriggered { get; set; }
        public DateTime? TriggeredAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
