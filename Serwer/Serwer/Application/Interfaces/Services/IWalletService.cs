using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface IWalletService
    {
        /// <summary>Creates a new wallet for the given user.</summary>
        Task<WalletDto> CreateWalletAsync(Guid userId, CreateWalletDto dto);

        /// <summary>Returns all wallets belonging to the given user.</summary>
        Task<IEnumerable<WalletDto>> GetUserWalletsAsync(Guid userId);

        /// <summary>Returns detailed information about a specific wallet, including assets.</summary>
        Task<WalletDetailsDto> GetWalletDetailsAsync(Guid userId, Guid walletId);

        /// <summary>Updates the name and description of a wallet owned by the user.</summary>
        Task<WalletDto> UpdateWalletAsync(Guid userId, Guid walletId, UpdateWalletDto dto);

        /// <summary>Deletes a wallet owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        Task DeleteWalletAsync(Guid userId, Guid walletId);
    }
}
