using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface IAuthService
    {
        /// <summary>Registers a new user and returns a JWT. Throws ArgumentException if email already exists.</summary>
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

        /// <summary>Authenticates a user and returns a JWT. Throws ArgumentException on invalid credentials.</summary>
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}
