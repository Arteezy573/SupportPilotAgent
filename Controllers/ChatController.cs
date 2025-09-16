using Microsoft.AspNetCore.Mvc;
using SupportPilotAgent.Configuration;
using SupportPilotAgent.Services;

namespace SupportPilotAgent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly SupportPilotAgent _agent;
        private readonly ResponseFormatterService _formatter;

        public ChatController(SupportPilotAgent agent, ResponseFormatterService formatter)
        {
            _agent = agent;
            _formatter = formatter;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            try
            {
                var rawResponse = await _agent.GenerateAsync(request.Message);
                var formattedResponse = _formatter.FormatResponse(rawResponse);
                return Ok(new ChatResponse { Response = formattedResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing message: {ex.Message}");
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}