namespace Investe.Application.DTOs
{
    public class HistoryPointDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
    }

    public class WalletHistoryDto
    {
        public Guid WalletId { get; set; }
        public List<HistoryPointDto> Points { get; set; } = new();
    }
}
