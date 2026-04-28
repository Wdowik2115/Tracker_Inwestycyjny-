using Serwer.Models.Identity;
using Serwer.Models.Transactions;

namespace Serwer.Models.Portfolio
{
    public class Wallet
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<Holding> Holdings { get; set; } = new List<Holding>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
