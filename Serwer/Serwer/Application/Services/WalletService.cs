using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Application.Mappings;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;

namespace Investe.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WalletService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<WalletDto> CreateAsync(string userId, string name, string description)
        {
            var wallet = new Wallet
            {
                UserId = userId,
                Name = name,
                Description = description
            };

            await _unitOfWork.Wallets.AddAsync(wallet);
            await _unitOfWork.CompleteAsync();

            return wallet.ToDto();
        }

        public async Task<IEnumerable<WalletDto>> GetUserWalletsAsync(string userId)
        {
            var wallets = await _unitOfWork.Wallets.GetWalletsByUserIdAsync(userId);
            return wallets.Select(w => w.ToDto());
        }

        public async Task<WalletDto?> GetDetailsAsync(int walletId)
        {
            var wallet = await _unitOfWork.Wallets.GetByIdAsync(walletId);
            return wallet?.ToDto();
        }
    }
}
