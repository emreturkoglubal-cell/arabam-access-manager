using Microsoft.AspNetCore.Mvc;
using AccessManager.UI.Services;

namespace AccessManager.UI.Controllers;

public class AiController : Controller
{
    private readonly IAiConversationService _aiConversation;

    public AiController(IAiConversationService aiConversation)
    {
        _aiConversation = aiConversation;
    }

    [HttpGet]
    public IActionResult Index([FromQuery] int? conversationId)
    {
        ViewData["ConversationId"] = conversationId;
        return View();
    }

    [HttpGet]
    public IActionResult GetConversations()
    {
        var list = _aiConversation.GetConversationsForCurrentUser();
        return Json(list.Select(c => new { id = c.Id, title = c.Title, updatedAt = c.UpdatedAt }));
    }

    [HttpGet]
    public IActionResult GetMessages([FromQuery] int conversationId)
    {
        var conv = _aiConversation.GetConversation(conversationId);
        if (conv == null)
            return Json(new { title = (string?)null, messages = Array.Empty<object>() });
        var list = _aiConversation.GetMessages(conversationId);
        return Json(new
        {
            title = conv.Title,
            messages = list.Select(m => new { role = m.Role, content = m.Content, createdAt = m.CreatedAt })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return Json(new { reply = "Lütfen bir mesaj yazın." });

        var (conversationId, title, reply) = await _aiConversation.SendMessageAsync(request.ConversationId, request.Message.Trim(), cancellationToken);
        return Json(new { conversationId, title, reply });
    }

    public class ChatRequest
    {
        public int? ConversationId { get; set; }
        public string? Message { get; set; }
    }
}
