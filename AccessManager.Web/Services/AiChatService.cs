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
    private const int ToolPreviewMaxChars = 320;
    private const int ArgsPreviewMaxChars = 240;
    private static readonly string[] ProgressOnlyPhrases =
    {
        "dosyayı okuyorum",
        "dosyayı inceliyorum",
        "inceliyorum",
        "bir saniye",
        "hemen bakıyorum",
        "değişikliği yapacağım",
        "değiştireceğim",
        "uygulayacağım",
        "yapacağım"
    };

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

    public async Task<string> SendAsync(
        string userMessage,
        IReadOnlyList<(string Role, string Content)>? previousMessages = null,
        int? conversationId = null,
        CancellationToken cancellationToken = default,
        Func<AiStreamEvent, CancellationToken, ValueTask>? onProgress = null)
    {
        ValueTask Emit(string type, string? message = null, int? round = null, string? toolName = null, string? argsPreview = null, string? resultPreview = null)
        {
            if (onProgress == null) return ValueTask.CompletedTask;
            return onProgress(new AiStreamEvent
            {
                Type = type,
                Message = message,
                Round = round,
                ToolName = toolName,
                ArgsPreview = argsPreview,
                ResultPreview = resultPreview
            }, cancellationToken);
        }

        var isFirstConversationTurn = previousMessages == null || previousMessages.Count == 0;

        var apiKey = _config["OpenAI:ApiKey"]?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("OpenAI API anahtarı tanımlı değil. OpenAI:ApiKey veya ortam değişkeni ayarlanmalı.");
            var msg = "OpenAI API anahtarı tanımlı değil. Lütfen yapılandırmada OpenAI:ApiKey veya ortam değişkeni ile verin.";
            await Emit("error", msg).ConfigureAwait(false);
            return msg;
        }

        if (isFirstConversationTurn)
            await Emit("phase", "Proje yapısı yükleniyor…").ConfigureAwait(false);
        string structure;
        try
        {
            structure = await _codeContext.GetProjectStructureAsync(cancellationToken).ConfigureAwait(false);
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

        if (isFirstConversationTurn)
            await Emit("phase", "Proje yapısı hazır.").ConfigureAwait(false);

        var relevantChunksBlock = "";
        if (_codeChunkSearch != null)
        {
            if (isFirstConversationTurn)
                await Emit("phase", "Kod parçaları için vektör araması yapılıyor…").ConfigureAwait(false);
            try
            {
                var hasIndex = await _codeChunkSearch.HasIndexAsync(cancellationToken).ConfigureAwait(false);
                if (hasIndex)
                {
                    var chunks = await _codeChunkSearch.GetRelevantChunksAsync(userMessage, VectorSearchTopK, cancellationToken).ConfigureAwait(false);
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
                        if (isFirstConversationTurn)
                            await Emit("phase", $"{chunks.Count} ilgili kod parçası bulundu.").ConfigureAwait(false);
                    }
                    else
                    {
                        if (isFirstConversationTurn)
                            await Emit("phase", "Vektör indeksinde eşleşen parça yok (veya boş).").ConfigureAwait(false);
                    }
                }
                else
                {
                    if (isFirstConversationTurn)
                        await Emit("phase", "Kod indeksi yok; vektör araması atlandı.").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AiChatService: Vektör araması atlandı.");
                if (isFirstConversationTurn)
                    await Emit("phase", "Vektör araması atlandı: " + ex.Message).ConfigureAwait(false);
            }
        }

        var systemContent = @"Sen Access Manager (arabam-access-manager) projesi için çalışan, kullanıcıyla canlı sohbet eden bir asistanısın. Cursor ile konuşuyormuş gibi doğal ve bilgilendirici ol.

ZORUNLU KURALLAR:
1) Selam, nasılsın, teşekkür, kısa hal hatır gibi mesajlara kısa ve sıcak yanıt ver; robotik reddetme yapma. Bu tür mesajlarda read_file veya başka araç çağırma. En az bir cümle doğal sohbet et; hemen ""projeye geçelim"" demek zorunda değilsin. İstersen en sonda yumuşakça kod veya Access Manager konusunda yardımcı olup olamayacağını sor.
2) Teknik sorular, kod incelemesi, mimari ve değişiklik talepleri için yanıtlarını bu repoya dayandır; read_file ile ilgili dosyaları oku. Tahmin yapma. Proje dışı genel konularda (başka ürünler, tıbbi/hukuki tavsiye, sınav cevabı vb.) uzun içerik verme; kısaca bu sohbetin Access Manager proje asistanı olduğunu belirtip projeye yönlendir.
3) Projeyi bozma veya tehlikeli toplu işlem (tüm dosyaları silmek, .git silmek vb.) kabul etme; reddedip nedenini açıkla. Basit ve güvenli istekleri (metin/label değiştirme, küçük UI düzeltmesi, typo düzeltmesi) reddetme; doğrudan uygula.
4) ""Bu isteği doğrudan uygulayamam"", ""güvenlik nedeniyle yapamam"" (basit kod/metin değişiklikleri için), ""önce onayını alayım mı?"" gibi gereksiz reddetme veya fazladan soru sorma. Kullanıcı net bir değişiklik istediyse araçları kullanıp uygula.

Tavır ve bilgilendirme:
- Ne yaptığını kısaca söyle: ""Dosyayı okuyorum..."", ""Değişikliği uyguluyorum..."", ""Build alıyorum..."", ""Pushlandı."" gibi.
- Hata olursa net açıkla: build hatası, push hatası, diff hatası. Kullanıcıya ne yapabileceğini öner (düzeltme, PR açma).
- Kullanıcı push istemiyorsa veya ""PR aç"" diyorsa create_pr kullan; main'e pushlama.

Araçlar:
- read_file: Dosya içeriği okumak. Path repo köküne göre. View'lar AccessManager.Web/Views/ altında.
- write_file: Yeni dosya veya tam içerik. Sonrasında kullanıcıya göster, onay al; onayda confirm_and_push veya create_pr.
- apply_diff: Mevcut dosyada unified diff uygula. Öncesinde mutlaka read_file ile güncel içeriği oku; bağlam satırlarını ("" "" ile başlayan context) dosyadan birebir kopyala, tahmin etme. Sonrasında kullanıcıya diff göster, push/PR için onay iste.
- run_build: Projeyi derler (dotnet build). confirm_and_push içinde de kullanılır. Gerçek derleme hatasında çıktıyı göster. Ortam sorunu (SDK yok, dosya kilidi/MSB302x) confirm_and_push tarafında push'u genelde bloklamaz.
- confirm_and_push: Kullanıcı ""Evet, pushla"" / ""Onayla"" / ""Pushla"" dediğinde çağır. Önce build dener; gerçek kod hatasında push etmez. SDK eksikliği veya başka işlem dosyayı kilitlediği için build kopyalama hatası gibi ortam sorunlarında build atlanabilir ve push devam edebilir (arabam AI yapılandırması).
- create_pr: Kullanıcı ""PR aç"", ""pull request aç"", ""pushlama PR aç"" veya doğrudan main'e push etmek istemediğini söylediğinde çağır. Değişiklikleri yeni branch'e commit edip push eder; kullanıcı GitHub/GitLab'da PR açar.
- git_commit_and_push: Sadece kullanıcı açıkça ""commit/push yap"" dediğinde (confirm_and_push dışında) kullan.
- propose_sql: Veritabanından salt okunur veri için kullan. Önce her zaman bunu çağır; tek SELECT (veya WITH…SELECT) ver. Bu araç SQL'i doğrular ve konuşmaya bekleyen sorgu olarak kaydeder. Asla doğrudan veritabanına yazma veya execute_pending_sql dışında SQL çalıştırma.
- execute_pending_sql: Parametresiz. Sadece kullanıcı sorguyu gördükten sonra açıkça onayladığında (""Evet, çalıştır"", ""Onaylıyorum"", ""Çalıştır"") çağır. Yalnızca bu konuşmada propose_sql ile kaydedilmiş sorguyu çalıştırır; istemciden veya argümanlardan ham SQL kabul etmez.

Salt okunur SQL akışı:
1) Veri sorusu: önce propose_sql ile geçerli bir SELECT öner; yanıttaki SQL'i kullanıcıya kod bloğunda göster ve çalıştırmadan önce onay iste.
2) Onay: kullanıcı onayladıktan sonra yalnızca execute_pending_sql çağır (başka SQL metni verme).
3) Onaysız execute_pending_sql kullanma.
4) execute_pending_sql araç çıktısı geldikten sonra kullanıcıya tek bir nihai Türkçe yanıt ver: sonuç tablosu boş veya 0 satırsa bunu açıkça söyle (""bu kriterlerde kayıt yok"", ""listede eşleşen satır yok"" gibi). Boş sonuç hataya değildir. Araç çıktısında **Sistem (yalnızca asistan)** bölümü varsa onu kullanıcıya gösterme; sadece anlamına uy. Aynı turda propose_sql ile aynı sorguyu TEKRAR önerme, tekrar onay isteme, execute_pending_sql'i yeniden çağırma.
5) Araç ""bekleyen sorgu yok"" / benzeri dönerse: sorgu çoktan çalıştırılmış veya iptal edilmiş olabilir. Aynı SQL ile yeniden onay bekleme; kullanıcıya durumu kısaca anlat. Gerekirse yeni bir soru için propose_sql ile farklı bir sorgu öner.
6) execute_pending_sql veya ReadOnlySqlQueryService ""ConnectionStrings:DefaultConnection"" / bağlantı yapılandırması eksik diyorsa: kullanıcıya ortam değişkeni veya yapılandırma eksikliğini net söyle; tahmini SQL ile devam etme.

Veritabanı SQL — PostgreSQL (ZORUNLU):
- Çalışan bağlantı PostgreSQL'dir. SQL içinde tablo ve sütun adları küçük harf ve snake_case kullanılır (örn. personnel, start_date). C# entity/sınıf adlarını (Personnel, StartDate) doğrudan SQL'de YAZMA.
- SQL Server / T-SQL kullanma: DATEADD, GETDATE(), TOP n … Bunların yerine PostgreSQL: CURRENT_DATE, NOW(), INTERVAL '1 year', LIMIT n.
- Kolon/tablo adından emin değilsen tahmin etme: read_file ile AccessManager.Domain/Entities/<İlgiliEntity>.cs ve AccessManager.Infrastructure/Repositories/<İlgili>Repository.cs aç; Repository içindeki SELECT … AS … ifadelerinde SQL tarafındaki gerçek adları görürsün.
- Vektör araması ile gelen kod parçaları yardımcıdır; şema için kesin kaynak Entity + Repository dosyalarıdır (indeks eski olabilir — şüphede read_file kullan).

Sık kullanılan personnel tablosu (SQL adları): personnel ( id, first_name, last_name, email, phone, department_id, position, manager_id, start_date, end_date, status, role_id, location, image_url, rating, manager_comment, seniority_level, team_id ). status sayısal enum: 0=Aktif, 1=Pasif, 2=Offboard.
Örnek son 1 yıl işe giriş sayısı: SELECT COUNT(*) AS total_hires FROM personnel WHERE start_date >= CURRENT_DATE - INTERVAL '1 year';

Kod değişikliği akışı:
1) Dosya düzenleme: ÖNCE read_file (hedef path kesin değilse proje yapısından bul). Tek/küçük değişiklikte sıra: read_file → apply_diff (küçük hunk; bağlam satırları az önce okunan dosyadan kopyala). apply_diff araçı ""HATA:"" dönerse aynı konuşma turunda: read_file ile yeniden oku → düzeltilmiş apply_diff dene; yine olmazsa write_file ile güncel tam içeriği yaz (son çare). Kullanıcıya önce ""Visual Studio'yu kapat"" deme; önce bu kurtarma adımlarını dene.
2) Değişiklik başarılı olunca diff veya özet kod bloğunda göster; hangi dosya(lar) değişti yaz. Sonra sor: ""Bu değişiklikleri main'e pushlamamı ister misiniz? ('Evet, pushla') Yoksa PR için branch oluşturayım mı? ('PR aç')""
3) ""Evet, pushla"" / ""Onayla"" → confirm_and_push. Gerçek derleme hatasında push olmaz; ortam (SDK / dosya kilidi) nedeniyle build atlandıysa push devam edebilir — kullanıcıya kısaca bildir, Cursor/VS kapat talimatını son çare olarak ver.
4) ""PR aç"" → create_pr.
5) confirm_and_push gerçek BUILD_HATA (kod) dönerse: hatayı göster; pushlanmadı de. Ortam nedeniyle build atlandıysa push başarılı mesajına uy.
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
        var progressOnlyRetryUsed = false;
        var applyDiffContextFailCount = 0;

        await Emit("phase", "Model ile konuşma başlıyor (araçlar kullanılabilir).").ConfigureAwait(false);

        while (round < MaxToolRoundTrips)
        {
            round++;
            await Emit("ping", "keep-alive").ConfigureAwait(false);
            await Emit("model_turn", "Yanıt bekleniyor…", round).ConfigureAwait(false);

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

            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var snippet = body.Length > 300 ? body[..300] + "..." : body;
                _logger.LogError("OpenAI API hatası. StatusCode: {StatusCode}, Body: {Body}", response.StatusCode, snippet);
                var err = $"API hatası ({(int)response.StatusCode}): " + snippet;
                await Emit("error", err).ConfigureAwait(false);
                return err;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(responseJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "AiChatService.SendAsync: OpenAI yanıtı JSON parse edilemedi. ResponseLength: {Length}", responseJson?.Length ?? 0);
                var err = lastContent ?? "Model yanıtı işlenemedi (geçersiz JSON).";
                await Emit("error", err).ConfigureAwait(false);
                return err;
            }
            using (doc)
            {
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                {
                    var err = lastContent ?? "Model cevap üretmedi.";
                    await Emit("error", err).ConfigureAwait(false);
                    return err;
                }

                var msg = choices[0].GetProperty("message");
                lastContent = msg.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String
                    ? c.GetString()
                    : null;

                if (!string.IsNullOrWhiteSpace(lastContent))
                    await Emit("phase", "Model kısa metin üretti; araç çağrısı yoksa bu yanıt nihai olabilir.", round).ConfigureAwait(false);

                if (!msg.TryGetProperty("tool_calls", out var toolCallsEl) || toolCallsEl.GetArrayLength() == 0)
                {
                    if (!progressOnlyRetryUsed && IsProgressOnlyAssistantReply(lastContent))
                    {
                        progressOnlyRetryUsed = true;
                        await Emit("phase", "Ara durum mesajı algılandı; işlem tamamlatılıyor…").ConfigureAwait(false);
                        messages.Add(new JsonObject
                        {
                            ["role"] = "user",
                            ["content"] = "Sadece ara durum yazma. Gerekli aracı çağırıp işi tamamla ve kullanıcıya nihai sonucu ver."
                        });
                        continue;
                    }
                    await Emit("phase", "Nihai yanıt hazır (araç zinciri bitti).").ConfigureAwait(false);
                    return lastContent ?? "(Boş cevap)";
                }

                await Emit("phase", $"{toolCallsEl.GetArrayLength()} araç çağrısı planlandı.", round).ConfigureAwait(false);

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
                    var argsPreview = BuildArgsPreview(name, argsStr);
                    await Emit("tool_start", message: null, round: round, toolName: name, argsPreview: argsPreview).ConfigureAwait(false);

                    var toolResult = await ExecuteToolAsync(name, argsStr, conversationId, cancellationToken).ConfigureAwait(false);

                    if (string.Equals(name, "apply_diff", StringComparison.OrdinalIgnoreCase))
                    {
                        if (toolResult.StartsWith("HATA: Bağlam bulunamadı", StringComparison.Ordinal))
                        {
                            applyDiffContextFailCount++;
                            if (applyDiffContextFailCount >= 2)
                            {
                                await Emit("phase", "Diff bağlamı tekrar kaçtı; write_file fallback zorlanıyor…", round).ConfigureAwait(false);
                                messages.Add(new JsonObject
                                {
                                    ["role"] = "user",
                                    ["content"] = "Aynı apply_diff hatası tekrarlandı. Bu dosya için artık apply_diff deneme. read_file ile güncel tam içeriği alıp yalnızca gerekli küçük metin değişimini yaparak write_file kullan ve işi tamamla."
                                });
                                applyDiffContextFailCount = 0;
                            }
                        }
                        else if (!toolResult.StartsWith("HATA:", StringComparison.Ordinal))
                        {
                            applyDiffContextFailCount = 0;
                        }
                    }

                    var resultPreview = BuildResultPreview(toolResult);
                    await Emit("tool_end", message: null, round: round, toolName: name, resultPreview: resultPreview).ConfigureAwait(false);

                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = id,
                        ["content"] = toolResult
                    });
                }
            }
        }

        await Emit("error", "Araç döngüsü üst sınırına ulaşıldı.").ConfigureAwait(false);
        return lastContent ?? "Araç döngüsü sınırına ulaşıldı. Özet: işlem yapıldı veya devam etmek için tekrar deneyin.";
    }

    private static string BuildArgsPreview(string toolName, string argumentsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;
            switch (toolName)
            {
                case "read_file":
                    if (root.TryGetProperty("path", out var p))
                        return Truncate(p.GetString() ?? "", ArgsPreviewMaxChars);
                    break;
                case "write_file":
                {
                    var path = root.TryGetProperty("path", out var wp) ? wp.GetString() ?? "" : "";
                    var len = root.TryGetProperty("content", out var ce) && ce.ValueKind == JsonValueKind.String
                        ? (ce.GetString() ?? "").Length
                        : 0;
                    return Truncate(path + $" (içerik ~{len} karakter)", ArgsPreviewMaxChars);
                }
                case "apply_diff":
                {
                    var path = root.TryGetProperty("path", out var ap) ? ap.GetString() ?? "" : "";
                    var dlen = root.TryGetProperty("diff", out var de) && de.ValueKind == JsonValueKind.String
                        ? (de.GetString() ?? "").Length
                        : 0;
                    return Truncate(path + $" (unified diff ~{dlen} karakter)", ArgsPreviewMaxChars);
                }
                case "git_commit_and_push":
                    if (root.TryGetProperty("commit_message", out var m))
                        return Truncate(m.GetString() ?? "", ArgsPreviewMaxChars);
                    break;
                case "propose_sql":
                    if (root.TryGetProperty("sql", out var sqlEl) && sqlEl.ValueKind == JsonValueKind.String)
                    {
                        var s = sqlEl.GetString() ?? "";
                        return Truncate($"{s.Length} karakter SQL", ArgsPreviewMaxChars);
                    }
                    break;
            }
        }
        catch
        {
            /* ignore */
        }

        return Truncate(argumentsJson.Replace('\r', ' ').Replace('\n', ' '), ArgsPreviewMaxChars);
    }

    private static string BuildResultPreview(string result)
    {
        if (string.IsNullOrEmpty(result)) return "(boş)";
        var oneLine = result.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return Truncate(oneLine, ToolPreviewMaxChars);
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s[..max] + "…";
    }

    private static bool IsProgressOnlyAssistantReply(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim();
        if (t.Length > 260) return false;
        if (t.Contains("```", StringComparison.Ordinal)) return false;
        var lower = t.ToLowerInvariant();
        for (var i = 0; i < ProgressOnlyPhrases.Length; i++)
        {
            if (lower.Contains(ProgressOnlyPhrases[i], StringComparison.Ordinal))
                return true;
        }
        return false;
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
                    return await _agentTools.WriteFileAsync(wPath, content, conversationId, cancellationToken).ConfigureAwait(false);
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
                    return await _agentTools.ApplyDiffAsync(path, diff, conversationId, cancellationToken).ConfigureAwait(false);
                }
                case "run_build":
                    return await _agentTools.RunBuildAsync(cancellationToken).ConfigureAwait(false);
                case "confirm_and_push":
                {
                    if (!conversationId.HasValue || conversationId.Value < 1)
                        return "HATA: Onay bekleyen değişiklik eşleştirilemedi (konuşma bilgisi yok).";
                    return await _agentTools.ConfirmPushAsync(conversationId.Value, cancellationToken).ConfigureAwait(false);
                }
                case "create_pr":
                {
                    if (!conversationId.HasValue || conversationId.Value < 1)
                        return "HATA: Bu konuşma için onay bekleyen değişiklik yok; önce değişiklik yapıp kullanıcıya 'PR aç' dedirt.";
                    return await _agentTools.CreatePrAsync(conversationId.Value, cancellationToken).ConfigureAwait(false);
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
                    return await _agentTools.GitCommitAndPushAsync(commitMessage, paths, cancellationToken).ConfigureAwait(false);
                }
                case "propose_sql":
                {
                    if (!conversationId.HasValue || conversationId.Value < 1)
                        return "HATA: Bu konuşma için sorgu kaydedilemedi (konuşma bilgisi yok).";
                    if (!root.TryGetProperty("sql", out var sqlEl))
                        return "HATA: 'sql' parametresi gerekli (tek SELECT veya WITH…SELECT).";
                    var sqlText = sqlEl.GetString() ?? "";
                    return await _agentTools.ProposeSqlAsync(sqlText, conversationId.Value, cancellationToken).ConfigureAwait(false);
                }
                case "execute_pending_sql":
                {
                    if (!conversationId.HasValue || conversationId.Value < 1)
                        return "HATA: Bekleyen sorgu çalıştırılamadı (konuşma bilgisi yok).";
                    return await _agentTools.ExecutePendingSqlAsync(conversationId.Value, cancellationToken).ConfigureAwait(false);
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
