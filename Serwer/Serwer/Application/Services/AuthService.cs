using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Investe.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration config)
        {
            _unitOfWork = unitOfWork;
            _config = config;
        }

        /// <summary>Registers a new user and returns a JWT. Throws ArgumentException if email already exists or password is too weak.</summary>
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var existing = await _unitOfWork.Users.GetByEmailAsync(dto.Email.ToLowerInvariant());
            if (existing != null)
                throw new ArgumentException("Email is already registered.");

            // Password validation
            if (dto.Password.Length < 8 ||
                !dto.Password.Any(char.IsUpper) ||
                !dto.Password.Any(char.IsDigit) ||
                !dto.Password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                throw new ArgumentException("Password does not meet requirements.");
            }

            var user = new User
            {
                Email = dto.Email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            return new AuthResponseDto
            {
                Token = GenerateJwt(user),
                UserId = user.Id,
                Email = user.Email
            };
        }

        /// <summary>Authenticates a user and returns a JWT. Throws ArgumentException on invalid credentials.</summary>
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email.ToLowerInvariant())
                ?? throw new ArgumentException("Invalid credentials.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new ArgumentException("Invalid credentials.");

            return new AuthResponseDto
            {
                Token = GenerateJwt(user),
                UserId = user.Id,
                Email = user.Email
            };
        }

        private string GenerateJwt(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpiryMinutes", 60));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
