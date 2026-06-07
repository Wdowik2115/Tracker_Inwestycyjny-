namespace Investe.Domain.Entities
{
    public class Wallet
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<User> SharedWith { get; set; } = new List<User>();
    }
}
