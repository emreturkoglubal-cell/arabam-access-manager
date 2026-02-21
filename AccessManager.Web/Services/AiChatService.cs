using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AccessManager.UI.Services.Agent;

namespace AccessManager.UI.Services;

public class AiChatService : IAiChatService
{
    private const int MaxToolRoundTrips = 10;
    private readonly IConfiguration _config;
    private readonly ICodeContextService _codeContext;
    private readonly IAgentTools _agentTools;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiChatService(
        IConfiguration config,
        ICodeContextService codeContext,
        IAgentTools agentTools,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _codeContext = codeContext;
        _agentTools = agentTools;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> SendAsync(string userMessage, IReadOnlyList<(string Role, string Content)>? previousMessages = null, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["OpenAI:ApiKey"]?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            return "OpenAI API anahtarı tanımlı değil. Lütfen yapılandırmada OpenAI:ApiKey veya ortam değişkeni ile verin.";

        var structure = await _codeContext.GetProjectStructureAsync(cancellationToken);
        var systemContent = @"Sen Access Manager projesi için repository-aware bir asistanısın. Hem soru cevaplayabilir hem de koda değişiklik yapıp main branch'e push edebilirsin. 

Araçlar:
- read_file: Bir dosyanın içeriğini okumak için. Path her zaman repo köküne göre. MVC view'lar AccessManager.Web/Views/ altındadır (Views kullan, Pages değil). Örn: AccessManager.Web/Views/Systems/Index.cshtml, AccessManager.Web/Views/Personnel/Index.cshtml.
- write_file: Yeni dosya veya tam içerik yazmak için.
- apply_diff: Mevcut dosyada değişiklik yapmak için unified diff uygula. Diff formatı: --- a/path, +++ b/path, @@ satır bilgisi, sonra + veya - veya boşluk ile başlayan satırlar.
- git_commit_and_push: Tüm değişiklikleri commit edip origin main'e push et. Sadece değiştirdiğin dosyaların path'lerini paths listesine ver.

Kod değişikliği isteniyorsa: Önce read_file ile ilgili dosyayı oku, sonra apply_diff ile değişikliği uygula, en sonda git_commit_and_push ile commit ve push yap. Commit mesajı Türkçe veya İngilizce kısa ve anlamlı olsun.
Sadece soru sorulduysa araç kullanmadan metinle cevap ver. Yanıtları Türkçe ver.

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
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return $"API hatası ({(int)response.StatusCode}): " + (body.Length > 300 ? body[..300] + "..." : body);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
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
                var name = fn.GetProperty("name").GetString();
                var argsStr = fn.GetProperty("arguments").GetString() ?? "{}";
                var toolResult = await ExecuteToolAsync(name, argsStr, cancellationToken); 
                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = id,
                    ["content"] = toolResult
                });
            }
        }

        return lastContent ?? "Araç döngüsü sınırına ulaşıldı. Özet: işlem yapıldı veya devam etmek için tekrar deneyin."; 
    }

    private async Task<string> ExecuteToolAsync(string name, string argumentsJson, CancellationToken cancellationToken)
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
                    return await _agentTools.WriteFileAsync(pathEl.GetString() ?? "", contentEl.GetString() ?? "", cancellationToken);
                }
                case "apply_diff":
                {
                    if (!root.TryGetProperty("path", out var pathEl))
                        return "HATA: 'path' parametresi gerekli (değiştirilecek dosya, repo köküne göre).";
                    if (!root.TryGetProperty("diff", out var diffEl))
                        return "HATA: 'diff' parametresi gerekli (unified diff: --- a/dosya, +++ b/dosya, @@ satırlar).";
                    var path = pathEl.GetString() ?? "";
                    var diff = diffEl.GetString() ?? "";
                    return await _agentTools.ApplyDiffAsync(path, diff, cancellationToken);
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
            return "HATA: " + ex.GetType().Name + " - " + ex.Message;
        }
    }
}
