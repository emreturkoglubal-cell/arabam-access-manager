using Microsoft.AspNetCore.Mvc;
using AccessManager.UI.Services;

namespace AccessManager.UI.Controllers;

public class AiController : Controller
{
    private readonly IAiChatService _aiChat;

    public AiController(IAiChatService aiChat)
    {
        _aiChat = aiChat;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return Json(new { reply = "Lütfen bir mesaj yazın." });

        var reply = await _aiChat.SendAsync(request.Message.Trim(), cancellationToken);
        return Json(new { reply });
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
    }
}
