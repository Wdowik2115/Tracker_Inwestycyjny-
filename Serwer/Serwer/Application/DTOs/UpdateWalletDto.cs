using System.ComponentModel.DataAnnotations;

namespace Investe.Application.DTOs
{
    public class UpdateWalletDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
