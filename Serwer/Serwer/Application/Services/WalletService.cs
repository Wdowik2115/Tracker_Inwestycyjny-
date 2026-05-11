using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Application.Mappings;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WalletService> _logger;

        public WalletService(IUnitOfWork unitOfWork, ILogger<WalletService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>Creates a new wallet for the given user.</summary>
        public async Task<WalletDto> CreateWalletAsync(Guid userId, CreateWalletDto dto)
        {
            var wallet = new Wallet
            {
                UserId = userId,
                Name = dto.Name,
                Description = dto.Description ?? string.Empty
            };

            await _unitOfWork.Wallets.AddAsync(wallet);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Wallet {WalletId} created for user {UserId}", wallet.Id, userId);
            return wallet.ToDto();
        }

        /// <summary>Returns all wallets belonging to the given user.</summary>
        public async Task<IEnumerable<WalletDto>> GetUserWalletsAsync(Guid userId)
        {
            var wallets = await _unitOfWork.Wallets.GetWalletsByUserIdAsync(userId);
            return wallets.Select(w => w.ToDto());
        }

        /// <summary>Deletes a wallet owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        public async Task DeleteWalletAsync(Guid userId, Guid walletId)
        {
            var wallet = await _unitOfWork.Wallets.GetByIdAsync(walletId)
                ?? throw new KeyNotFoundException($"Wallet {walletId} not found.");

            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Wallet does not belong to this user.");

            await _unitOfWork.Wallets.DeleteAsync(wallet);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Wallet {WalletId} deleted by user {UserId}", walletId, userId);
        }
    }
}
