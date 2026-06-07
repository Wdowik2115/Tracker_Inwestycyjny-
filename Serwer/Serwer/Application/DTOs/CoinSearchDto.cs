namespace Investe.Application.DTOs
{
    public class CoinSearchDto
    {
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? Rank { get; set; }
    }
}
