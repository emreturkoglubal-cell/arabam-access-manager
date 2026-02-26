using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AccessManager.UI.Services.Agent;
using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Services;

public class AiChatService : IAiChatService
{
    private const int MaxToolRoundTrips = 10;
    private const int VectorSearchTopK = 10;

    private readonly IConfiguration _config;
    private readonly ICodeContextService _codeContext;
    private readonly ICodeChunkSearchService? _codeChunkSearch;
    private readonly IAgentTools _agentTools;
    private readonly IPendingPushStore _pendingPush;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(
        IConfiguration config,
        ICodeContextService codeContext,
        IAgentTools agentTools,
        IPendingPushStore pendingPush,
        IHttpClientFactory httpClientFactory,
        ILogger<AiChatService> logger,
        ICodeChunkSearchService? codeChunkSearch = null)
    {
        _config = config;
        _codeContext = codeContext;
        _agentTools = agentTools;
        _pendingPush = pendingPush;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _codeChunkSearch = codeChunkSearch;
    }

    public async Task<string> SendAsync(string userMessage, IReadOnlyList<(string Role, string Content)>? previousMessages = null, int? conversationId = null, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["OpenAI:ApiKey"]?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("OpenAI API anahtarı tanımlı değil. OpenAI:ApiKey veya ortam değişkeni ayarlanmalı.");
            return "OpenAI API anahtarı tanımlı değil. Lütfen yapılandırmada OpenAI:ApiKey veya ortam değişkeni ile verin.";
        }

        string structure;
        try
        {
            structure = await _codeContext.GetProjectStructureAsync(cancellationToken);
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogError(ex, "AiChatService.SendAsync: Proje yapısı alınırken bellek yetersiz (OOM). GetProjectStructureAsync.");
            structure = "# Proje yapısı bellek sınırı nedeniyle yüklenemedi. Genel soruları yanıtlayabilirim.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiChatService.SendAsync: Proje yapısı alınırken hata. GetProjectStructureAsync.");
            structure = "# Proje yapısı yüklenemedi: " + ex.Message;
        }

        var relevantChunksBlock = "";
        if (_codeChunkSearch != null)
        {
            try
            {
                var hasIndex = await _codeChunkSearch.HasIndexAsync(cancellationToken);
                if (hasIndex)
                {
                    var chunks = await _codeChunkSearch.GetRelevantChunksAsync(userMessage, VectorSearchTopK, cancellationToken);
                    if (chunks.Count > 0)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("\n--- Soruya en alakalı kod parçaları (vektör araması) ---");
                        sb.AppendLine("Cevaplarını önce bu parçalara dayandır; gerekirse read_file ile tam dosyayı oku.");
                        foreach (var (path, content) in chunks)
                        {
                            sb.AppendLine("\n## " + path);
                            sb.AppendLine(content);
                        }
                        relevantChunksBlock = sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AiChatService: Vektör araması atlandı.");
            }
        }

        var systemContent = @"Sen Access Manager (arabam-access-manager) projesi için çalışan, kullanıcıyla canlı sohbet eden bir asistanısın. Cursor ile konuşuyormuş gibi doğal ve bilgilendirici ol.

ZORUNLU KURALLAR:
1) Sadece bu projeyle ilgili sorulara cevap ver. Proje dışı konularda kısa ve nazikçe 'Bu asistan yalnızca Access Manager projesiyle ilgili soruları yanıtlar.' de.
2) Cevaplarını projenin kaynak koduna dayandır; read_file ile ilgili dosyaları oku. Tahmin yapma.
3) Projeyi bozma veya tehlikeli toplu işlem (tüm dosyaları silmek, .git silmek vb.) kabul etme; reddedip nedenini açıkla.

Tavır ve bilgilendirme:
- Ne yaptığını kısaca söyle: ""Dosyayı okuyorum..."", ""Değişikliği uyguluyorum..."", ""Build alıyorum..."", ""Pushlandı."" gibi.
- Hata olursa net açıkla: build hatası, push hatası, diff hatası. Kullanıcıya ne yapabileceğini öner (düzeltme, PR açma).
- Kullanıcı push istemiyorsa veya ""PR aç"" diyorsa create_pr kullan; main'e pushlama.

Araçlar:
- read_file: Dosya içeriği okumak. Path repo köküne göre. View'lar AccessManager.Web/Views/ altında.
- write_file: Yeni dosya veya tam içerik. Sonrasında kullanıcıya göster, onay al; onayda confirm_and_push veya create_pr.
- apply_diff: Mevcut dosyada unified diff uygula. Sonrasında kullanıcıya diff göster, onay al.
- run_build: Projeyi derler (dotnet build). confirm_and_push zaten içinde build alır; ayrıca kullanıcı ""build al"" derse de çağır. Build hata verirse çıktıyı kullanıcıya göster, pushlama.
- confirm_and_push: Kullanıcı ""Evet, pushla"" / ""Onayla"" / ""Pushla"" dediğinde çağır. Önce build alır; build başarısızsa push etmez ve hatayı bildirir. Başarılıysa main'e commit+push.
- create_pr: Kullanıcı ""PR aç"", ""pull request aç"", ""pushlama PR aç"" veya doğrudan main'e push etmek istemediğini söylediğinde çağır. Değişiklikleri yeni branch'e commit edip push eder; kullanıcı GitHub/GitLab'da PR açar.
- git_commit_and_push: Sadece kullanıcı açıkça ""commit/push yap"" dediğinde (confirm_and_push dışında) kullan.

Kod değişikliği akışı:
1) read_file ile ilgili dosyayı oku, apply_diff veya write_file ile değiştir.
2) Değişikliği (diff veya özet) kod bloğunda göster, hangi dosya(lar)ı değiştirdiğini yaz. Sonra sor: ""Bu değişiklikleri main'e pushlamamı ister misiniz? ('Evet, pushla') Yoksa PR için branch oluşturayım mı? ('PR aç')""
3) ""Evet, pushla"" / ""Onayla"" → confirm_and_push (içinde build alınır; build hata verirse kullanıcıya söyle, pushlama).
4) ""PR aç"" / ""Pull request"" / ""Pushlama, PR aç"" → create_pr.
5) confirm_and_push sonrası build hatası dönerse: Kullanıcıya hatayı açıkla, ""Değişiklikler pushlanmadı. Hatayı düzelttikten sonra tekrar 'Evet, pushla' diyebilir veya 'PR aç' ile sadece branch oluşturup PR açabilirsiniz."" de.
Sadece soru sorulduysa: read_file ile kaynak okuyup cevap ver. Yanıtları Türkçe ver.
" + relevantChunksBlock + @"

--- Proje yapısı (path'ler repo köküne göre) ---
" + structure;

        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = systemContent }
        };
        if (previousMessages != null)
        {
            foreach (var m in previousMessages)
                if (m.Role == "user" || m.Role == "assistant")
                    messages.Add(new JsonObject { ["role"] = m.Role, ["content"] = m.Content });
        }
        messages.Add(new JsonObject { ["role"] = "user", ["content"] = userMessage });

        var model = _config["OpenAI:Model"] ?? "gpt-4o-mini";
        var temperature = _config.GetValue("OpenAI:Temperature", 0.2);
        var round = 0;
        string? lastContent = null;

        while (round < MaxToolRoundTrips)
        {
            round++;
            // Her istekte messages'ın kopyasını kullan; aynı node iki payload'a atanınca "node already has a parent" hatası oluyor.
            var messagesCopy = JsonSerializer.Deserialize<JsonArray>(messages.ToJsonString())!;
            var payload = new JsonObject
            {
                ["model"] = model,
                ["messages"] = messagesCopy,
                ["temperature"] = temperature,
                ["max_tokens"] = 4096,
                ["tools"] = OpenAiToolDefinitions.GetToolsJson(),
                ["tool_choice"] = "auto"
            };

            var json = payload.ToJsonString();
            var client = _httpClientFactory.CreateClient("OpenAI");
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var snippet = body.Length > 300 ? body[..300] + "..." : body;
                _logger.LogError("OpenAI API hatası. StatusCode: {StatusCode}, Body: {Body}", response.StatusCode, snippet);
                return $"API hatası ({(int)response.StatusCode}): " + snippet;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(responseJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "AiChatService.SendAsync: OpenAI yanıtı JSON parse edilemedi. ResponseLength: {Length}", responseJson?.Length ?? 0);
                return lastContent ?? "Model yanıtı işlenemedi (geçersiz JSON).";
            }
            using (doc)
            {
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                    return lastContent ?? "Model cevap üretmedi.";

                var msg = choices[0].GetProperty("message");
                lastContent = msg.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String
                    ? c.GetString()
                    : null;

                if (!msg.TryGetProperty("tool_calls", out var toolCallsEl) || toolCallsEl.GetArrayLength() == 0)
                    return lastContent ?? "(Boş cevap)";

                var assistantMsg = new JsonObject { ["role"] = "assistant", ["content"] = lastContent ?? "" };
                var toolCallsArray = new JsonArray();
                foreach (var tc in toolCallsEl.EnumerateArray())
                {
                    var id = tc.GetProperty("id").GetString();
                    var fn = tc.GetProperty("function");
                    var name = fn.GetProperty("name").GetString();
                    var argsStr = fn.GetProperty("arguments").GetString() ?? "{}";
                    toolCallsArray.Add(new JsonObject
                    {
                        ["id"] = id,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = name,
                            ["arguments"] = argsStr
                        }
                    });
                }
                assistantMsg["tool_calls"] = toolCallsArray;
                messages.Add(assistantMsg);

                foreach (var tc in toolCallsEl.EnumerateArray())
                {
                    var id = tc.GetProperty("id").GetString();
                    var fn = tc.GetProperty("function");
                    var name = fn.GetProperty("name").GetString() ?? "";
                    var argsStr = fn.GetProperty("arguments").GetString() ?? "{}";
                    var toolResult = await ExecuteToolAsync(name, argsStr, conversationId, cancellationToken);
                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = id,
                        ["content"] = toolResult
                    });
                }
            }
        }

        return lastContent ?? "Araç döngüsü sınırına ulaşıldı. Özet: işlem yapıldı veya devam etmek için tekrar deneyin."; 
    }

    private async Task<string> ExecuteToolAsync(string name, string argumentsJson, int? conversationId, CancellationToken cancellationToken)
    {
        JsonElement root;
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            root = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            return "HATA: Geçersiz JSON argümanlar. Örnek: {\"path\": \"dosya/yolu\"} veya {\"path\": \"...\", \"diff\": \"...\"}. Detay: " + ex.Message;
        }

        try
        {
            switch (name)
            {
                case "read_file":
                {
                    if (!root.TryGetProperty("path", out var pathEl))
                        return "HATA: 'path' parametresi gerekli. Örnek: {\"path\": \"AccessManager.Web/Views/Systems/Index.cshtml\"}";
                    var path = pathEl.GetString() ?? "";
                    return _agentTools.ReadFile(path);
                }
                case "write_file":
                {
                    if (!root.TryGetProperty("path", out var pathEl))
                        return "HATA: 'path' parametresi gerekli.";
                    if (!root.TryGetProperty("content", out var contentEl))
                        return "HATA: 'content' parametresi gerekli.";
                    var wPath = pathEl.GetString() ?? "";
                    var content = contentEl.GetString() ?? "";
                    return await _agentTools.WriteFileAsync(wPath, content, conversationId, cancellationToken);
                }
                case "apply_diff":
                {
                    if (!root.TryGetProperty("path", out var pathEl))
                        return "HATA: 'path' parametresi gerekli (değiştirilecek dosya, repo köküne göre).";
                    if (!root.TryGetProperty("diff", out var diffEl))
                        return "HATA: 'diff' parametresi gerekli (unified diff: --- a/dosya, +++ b/dosya, @@ satırlar).";
                    var path = pathEl.GetString() ?? "";
                    var diff = diffEl.GetString() ?? "";
                    _logger.LogError("AiChatService.apply_diff: path: {Path}, diff (ilk 1000 karakter): {DiffPreview}", path, diff.Length > 1000 ? diff[..1000] + "..." : diff);
                    return await _agentTools.ApplyDiffAsync(path, diff, conversationId, cancellationToken);
                }
                case "run_build":
                    return await _agentTools.RunBuildAsync(cancellationToken);
                case "confirm_and_push":
                {
                    if (!conversationId.HasValue || conversationId.Value < 1)
                        return "HATA: Onay bekleyen değişiklik eşleştirilemedi (konuşma bilgisi yok).";
                    return await _agentTools.ConfirmPushAsync(conversationId.Value, cancellationToken);
                }
                case "create_pr":
                {
                    if (!conversationId.HasValue || conversationId.Value < 1)
                        return "HATA: Bu konuşma için onay bekleyen değişiklik yok; önce değişiklik yapıp kullanıcıya 'PR aç' dedirt.";
                    return await _agentTools.CreatePrAsync(conversationId.Value, cancellationToken);
                }
                case "git_commit_and_push":
                {
                    if (!root.TryGetProperty("commit_message", out var msgEl))
                        return "HATA: 'commit_message' parametresi gerekli.";
                    var commitMessage = msgEl.GetString() ?? "";
                    var paths = new List<string>();
                    if (root.TryGetProperty("paths", out var arr))
                    {
                        foreach (var p in arr.EnumerateArray())
                            paths.Add(p.GetString() ?? "");
                    }
                    return await _agentTools.GitCommitAndPushAsync(commitMessage, paths, cancellationToken);
                }
                default:
                    return "HATA: Bilinmeyen araç: " + name;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiChatService.ExecuteToolAsync: Araç çalıştırma hatası. Tool: {ToolName}, ArgumentsLength: {Length}", name, argumentsJson?.Length ?? 0);
            return "HATA: " + ex.GetType().Name + " - " + ex.Message;
        }
    }
}
