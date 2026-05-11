using System.ComponentModel.DataAnnotations;

namespace Investe.Application.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
    }
}
