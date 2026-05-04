using System.Collections.Concurrent;

namespace AccessManager.UI.Services.Agent;

public sealed class PendingSqlStore : IPendingSqlStore
{
    private readonly ConcurrentDictionary<int, string> _store = new();

    public void Set(int conversationId, string normalizedSql)
    {
        if (conversationId < 1 || string.IsNullOrWhiteSpace(normalizedSql)) return;
        _store[conversationId] = normalizedSql.Trim();
    }

    public string? Get(int conversationId) =>
        _store.TryGetValue(conversationId, out var s) ? s : null;

    public void Clear(int conversationId) => _store.TryRemove(conversationId, out _);
}
