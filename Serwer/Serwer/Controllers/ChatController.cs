using Investe.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serwer.Extensions;
using Microsoft.Extensions.Logging;

namespace Serwer.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question cannot be empty.");

            try 
            {
                var response = await _chatService.AskQuestionAsync(userId, request.Question);
                return Ok(new { response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatController Error");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = User.GetUserId();
            var history = await _chatService.GetHistoryAsync(userId);
            return Ok(history);
        }

        [HttpDelete("history")]
        public async Task<IActionResult> ClearHistory()
        {
            var userId = User.GetUserId();
            await _chatService.ClearHistoryAsync(userId);
            return NoContent();
        }
    }

    public class ChatRequest
    {
        public string Question { get; set; } = string.Empty;
    }
}
