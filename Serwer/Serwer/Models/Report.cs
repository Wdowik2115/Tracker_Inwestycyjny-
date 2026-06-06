namespace Investe.Domain.Entities
{
    public class Report
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid? WalletId { get; set; }
        public Wallet? Wallet { get; set; }
        public ReportType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public long FileSizeBytes { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
