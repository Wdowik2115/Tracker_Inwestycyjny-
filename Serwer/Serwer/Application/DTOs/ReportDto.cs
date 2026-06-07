namespace Investe.Application.DTOs
{
    public class ReportDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? WalletName { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
