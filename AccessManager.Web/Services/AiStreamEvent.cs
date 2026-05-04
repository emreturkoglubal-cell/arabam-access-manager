using System.Text.Json;
using System.Text.Json.Serialization;

namespace AccessManager.UI.Services;

/// <summary>NDJSON satırı olarak istemciye gönderilen arabam AI canlı durum olayı.</summary>
public sealed class AiStreamEvent
{
    public string Type { get; init; } = "";

    /// <summary>phase / error için kısa Türkçe açıklama.</summary>
    public string? Message { get; init; }

    public int? Round { get; init; }
    public string? ToolName { get; init; }

    /// <summary>Araç argümanlarının güvenli özeti (tam diff içeriği yok).</summary>
    public string? ArgsPreview { get; init; }

    /// <summary>Araç çıktısının kısaltılmış tek satır özeti.</summary>
    public string? ResultPreview { get; init; }

    /// <summary>Sadece type=done için.</summary>
    public int? ConversationId { get; init; }

    public string? Title { get; init; }
    public string? Reply { get; init; }

    public string? TimestampUtc { get; init; } = DateTime.UtcNow.ToString("o");
}

public static class AiStreamSerialization
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
