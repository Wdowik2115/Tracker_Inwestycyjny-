using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serwer.Extensions;

namespace Serwer.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions()
        {
            var transactions = await _transactionService.GetUserTransactionsAsync(User.GetUserId());
            return Ok(transactions);
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction([FromBody] TransactionCreateDto dto)
        {
            var transaction = await _transactionService.AddTransactionAsync(User.GetUserId(), dto);
            return CreatedAtAction(nameof(GetTransactions), new { }, transaction);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] TransactionUpdateDto dto)
        {
            var result = await _transactionService.UpdateTransactionAsync(User.GetUserId(), id, dto);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            await _transactionService.DeleteTransactionAsync(User.GetUserId(), id);
            return NoContent();
        }
    }
}
