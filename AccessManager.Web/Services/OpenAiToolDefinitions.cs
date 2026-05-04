using System.Text.Json.Nodes;

namespace AccessManager.UI.Services;

/// <summary>
/// OpenAI Chat Completions API için tool (function) tanımları.
/// </summary>
public static class OpenAiToolDefinitions
{
    public static JsonArray GetToolsJson()
    {
        return new JsonArray
        {
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "read_file",
                    ["description"] = "Repo köküne göre relative path ile bir dosyanın içeriğini okur. Örn: AccessManager.Web/Views/Personnel/Index.cshtml",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Repo köküne göre dosya yolu" }
                        },
                        ["required"] = new JsonArray { "path" }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "write_file",
                    ["description"] = "Belirtilen path'e dosya içeriğini yazar. Yeni dosya veya tam üzerine yazma.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Repo köküne göre dosya yolu" },
                            ["content"] = new JsonObject { ["type"] = "string", ["description"] = "Dosya içeriği" }
                        },
                        ["required"] = new JsonArray { "path", "content" }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "apply_diff",
                    ["description"] = "Unified diff formatında değişikliği dosyaya uygular. Path repo köküne göre olmalı.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Hedef dosya yolu (repo köküne göre)" },
                            ["diff"] = new JsonObject { ["type"] = "string", ["description"] = "Unified diff metni (--- a/path, +++ b/path, @@ ... ile başlar)" }
                        },
                        ["required"] = new JsonArray { "path", "diff" }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "git_commit_and_push",
                    ["description"] = "Sadece kullanıcı açıkça commit/push istediğinde kullan. Kod değişikliği yaptıktan sonra ÖNCE kullanıcıya gösterip onay al, onayda confirm_and_push kullan.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["commit_message"] = new JsonObject { ["type"] = "string", ["description"] = "Commit mesajı" },
                            ["paths"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" }, ["description"] = "Commit edilecek dosya yolları (repo köküne göre)" }
                        },
                        ["required"] = new JsonArray { "commit_message", "paths" }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "run_build",
                    ["description"] = "Projeyi derler (dotnet build). confirm_and_push öncesi mutlaka çağır. Build hata verirse kullanıcıya çıktıyı göster, pushlama. Parametre yok.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject { },
                        ["required"] = new JsonArray { }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "confirm_and_push",
                    ["description"] = "Kullanıcı 'Evet, pushla' / 'Onayla' dediğinde çağır. Önce build alır (başarısızsa push etmez), sonra bekleyen değişiklikleri commit edip main'e push eder. Parametre yok.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject { },
                        ["required"] = new JsonArray { }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "create_pr",
                    ["description"] = "Kullanıcı 'PR aç', 'pull request aç', 'pushlama PR aç' veya doğrudan main'e push etmek yerine PR istediğinde çağır. Bekleyen değişiklikleri yeni branch'e commit edip push eder; kullanıcı GitHub/GitLab'da PR açar. Parametre yok.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject { },
                        ["required"] = new JsonArray { }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "propose_sql",
                    ["description"] = "Veritabanından salt okunur veri almak için kullan. Önce HER ZAMAN bunu çağır: tek bir SELECT (veya WITH…SELECT) metni ver. Veritabanı PostgreSQL'dir; tablo/kolon adları snake_case (örn. personnel.start_date). SQL Server sözdizimi kullanma (DATEADD, GETDATE vb.). Şema emin değilsen read_file ile AccessManager.Domain/Entities ve AccessManager.Infrastructure/Repositories ilgili *Repository.cs dosyasına bak. Yazma yok. SQL doğrulanır, LIMIT 1000. Asla doğrudan DB'ye yazma. Kullanıcıya SQL'i göster, onay iste; onaydan sonra yalnızca execute_pending_sql.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["sql"] = new JsonObject { ["type"] = "string", ["description"] = "Tek ifadeli SELECT veya WITH…SELECT sorgusu" }
                        },
                        ["required"] = new JsonArray { "sql" }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "execute_pending_sql",
                    ["description"] = "Kullanıcı 'Evet çalıştır', 'Onaylıyorum', 'Çalıştır' gibi açıkça onayladıktan sonra çağır. Yalnızca bu konuşmada propose_sql ile kaydedilmiş doğrulanmış SELECT'i çalıştırır; istemciden ham SQL almaz. Parametre yok.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject { },
                        ["required"] = new JsonArray { }
                    }
                }
            }
        };
    }
}
