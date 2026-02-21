using Microsoft.AspNetCore.Mvc;
using AccessManager.UI.Services;

namespace AccessManager.UI.Controllers;

public class AiController : Controller
{
    private readonly IAiConversationService _aiConversation;
    private readonly ILogger<AiController> _logger;

    public AiController(IAiConversationService aiConversation, ILogger<AiController> logger)
    {
        _aiConversation = aiConversation;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index([FromQuery] int? conversationId)
    {
        ViewData["ConversationId"] = conversationId;
        return View();
    }

    [HttpGet]
    public IActionResult GetConversations([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var (items, total) = _aiConversation.GetConversationsPaged(skip, Math.Min(take, 50));
        return Json(new
        {
            items = items.Select(c => new { id = c.Id, title = c.Title, updatedAt = c.UpdatedAt }),
            total,
            hasMore = skip + items.Count < total
        });
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

        try
        {
            var (conversationId, title, reply) = await _aiConversation.SendMessageAsync(request.ConversationId, request.Message.Trim(), cancellationToken);
            return Json(new { conversationId, title, reply });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Chat hatası. ConversationId: {ConversationId}", request.ConversationId);
            return Json(new { reply = "Sunucu hatası: " + ex.Message });
        }
    }

    public class ChatRequest
    {
        public int? ConversationId { get; set; }
        public string? Message { get; set; }
    }
}
