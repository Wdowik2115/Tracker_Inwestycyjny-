using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface ITransactionService
    {
        /// <summary>Adds a buy/sell transaction for the user's wallet, auto-filling cost basis when missing.</summary>
        Task<TransactionDto> AddTransactionAsync(Guid userId, TransactionCreateDto dto);

        /// <summary>Returns all transactions across all wallets owned by the user.</summary>
        Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(Guid userId);

        /// <summary>Deletes a transaction owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        Task DeleteTransactionAsync(Guid userId, Guid transactionId);

        /// <summary>Updates editable metadata fields of a transaction. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        Task<TransactionDto> UpdateTransactionAsync(Guid userId, Guid transactionId, TransactionUpdateDto dto);
    }
}
