using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<UserDto> GetProfileAsync(Guid userId);
        Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    }
}
