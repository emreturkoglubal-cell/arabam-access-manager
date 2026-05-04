using System.Text.Json;
using AccessManager.UI.Constants;
using AccessManager.UI.Services;
using AccessManager.UI.Services.Agent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace AccessManager.UI.Controllers;

/// <summary>
/// AI asistan sayfası: proje kaynak koduna dayalı soru-cevap ve isteğe bağlı kod değişikliği (read_file, apply_diff, git commit/push).
/// Konuşmalar conversationId ile saklanır; GetConversations, GetMessages ile listelenir, Chat POST ile mesaj gönderilir.
/// Reindex: RAG vektör indexini (pgvector) yeniden oluşturur (Admin).
/// </summary>
public class AiController : Controller
{
    private readonly IAiConversationService _aiConversation;
    private readonly IPendingSqlStore _pendingSql;
    private readonly CodeChunkIndexService? _codeChunkIndex;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiConversationService aiConversation,
        IPendingSqlStore pendingSql,
        ILogger<AiController> logger,
        CodeChunkIndexService? codeChunkIndex = null)
    {
        _aiConversation = aiConversation;
        _pendingSql = pendingSql;
        _logger = logger;
        _codeChunkIndex = codeChunkIndex;
    }

    /// <summary>GET /Ai/Index — AI sohbet sayfası; conversationId ile mevcut konuşma açılabilir.</summary>
    [HttpGet]
    public IActionResult Index([FromQuery] int? conversationId)
    {
        ViewData["ConversationId"] = conversationId;
        return View();
    }

    /// <summary>GET /Ai/GetConversations — Konuşma listesini sayfalı döner (JSON).</summary>
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

    /// <summary>GET /Ai/GetMessages — Belirtilen konuşmanın mesajlarını döner (JSON). Tek round-trip: conversation + messages.</summary>
    [HttpGet]
    public IActionResult GetMessages([FromQuery] int conversationId)
    {
        var (title, list) = _aiConversation.GetConversationWithMessages(conversationId);
        if (title == null)
            return Json(new { title = (string?)null, messages = Array.Empty<object>() });

        return Json(new
        {
            title,
            messages = list.Select(m => new { role = m.Role, content = m.Content, createdAt = m.CreatedAt })
        });
    }

    /// <summary>POST /Ai/ChatStream — NDJSON satırları: phase, model_turn, tool_start, tool_end, ping, error, done (son).</summary>
    [HttpPost]
    public async Task ChatStream([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        Response.Headers.Append("X-Accel-Buffering", "no");
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        async ValueTask WriteEvent(AiStreamEvent ev, CancellationToken ct)
        {
            var line = JsonSerializer.Serialize(ev, AiStreamSerialization.JsonOptions) + "\n";
            await Response.WriteAsync(line, ct).ConfigureAwait(false);
            await Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            await WriteEvent(new AiStreamEvent { Type = "error", Message = "Lütfen bir mesaj yazın." }, cancellationToken).ConfigureAwait(false);
            await WriteEvent(new AiStreamEvent { Type = "done", ConversationId = 0, Title = "", Reply = "Lütfen bir mesaj yazın." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            await _aiConversation.SendMessageStreamAsync(request.ConversationId, request.Message.Trim(), WriteEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await WriteEvent(new AiStreamEvent { Type = "error", Message = "İstek iptal edildi." }, CancellationToken.None).ConfigureAwait(false);
            await WriteEvent(new AiStreamEvent { Type = "done", ConversationId = 0, Title = "", Reply = "İstek iptal edildi." }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI ChatStream hatası. ConversationId: {ConversationId}", request.ConversationId);
            var msg = "Sunucu hatası: " + ex.Message;
            await WriteEvent(new AiStreamEvent { Type = "error", Message = msg }, cancellationToken).ConfigureAwait(false);
            await WriteEvent(new AiStreamEvent { Type = "done", ConversationId = 0, Title = "", Reply = msg }, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>POST /Ai/Chat — Kullanıcı mesajını AI'a gönderir; yanıt ve güncel conversationId/title JSON döner.</summary>
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
            _logger.LogError(ex, "AI Chat hatası. ConversationId: {ConversationId}, ExceptionType: {ExceptionType}, Message: {Message}", request.ConversationId, ex.GetType().FullName, ex.Message);
            return Json(new { reply = "Sunucu hatası: " + ex.Message });
        }
    }

    /// <summary>POST /Ai/CancelPendingSql — Kullanıcının bu konuşmadaki bekleyen onaylı SQL kaydını siler (onaylamıyorum akışı).</summary>
    [HttpPost]
    public IActionResult CancelPendingSql([FromBody] ChatRequest? request)
    {
        var id = request?.ConversationId;
        if (id is null or < 1)
            return Json(new { success = false, message = "Geçersiz konuşma." });
        if (_aiConversation.GetConversation(id.Value) == null)
            return Json(new { success = false, message = "Sohbet bulunamadı veya yetkiniz yok." });
        _pendingSql.Clear(id.Value);
        return Json(new { success = true });
    }

    /// <summary>POST /Ai/DeleteConversation — Sohbeti soft delete yapar (is_active = false). Sadece kendi sohbeti silinebilir.</summary>
    [HttpPost]
    public IActionResult DeleteConversation([FromQuery] int conversationId)
    {
        if (conversationId <= 0)
            return Json(new { success = false, message = "Geçersiz konuşma." });
        var ok = _aiConversation.DeleteConversation(conversationId);
        return Json(new { success = ok, message = ok ? "Sohbet silindi." : "Sohbet bulunamadı veya yetkiniz yok." });
    }

    /// <summary>POST /Ai/Reindex — RAG vektör indexini yeniden oluşturur (repo tarama + embedding + pgvector). Sadece Admin.</summary>
    [HttpGet]
    [Authorize(Roles = AuthorizationRolePolicies.AdminOnly)]
    public async Task<IActionResult> Reindex()
    {
        if (_codeChunkIndex == null)
            return Json(new { success = false, message = "CodeChunkIndexService kayıtlı değil.", count = 0 });
        try
        {
            var count = await _codeChunkIndex.ReindexAsync();
            return Json(new { success = true, message = $"Index güncellendi. {count} parça yazıldı.", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Reindex hatası.");
            return Json(new { success = false, message = ex.Message, count = 0 });
        }
    }

    /// <summary>AI sohbet isteği: mevcut konuşma ID (yoksa yeni açılır) ve kullanıcı mesajı.</summary>
    public class ChatRequest
    {
        public int? ConversationId { get; set; }
        public string? Message { get; set; }
    }
}
