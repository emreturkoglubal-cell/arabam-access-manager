using System.Collections.Concurrent;

namespace AccessManager.UI.Services.Agent;

public sealed class PendingPushStore : IPendingPushStore
{
    private readonly ConcurrentDictionary<int, (IReadOnlyList<string> Paths, string CommitMessage, string? DiffOrSummary)> _store = new();

    public void Set(int conversationId, IReadOnlyList<string> paths, string commitMessage, string? diffOrSummary = null)
    {
        if (paths.Count == 0) return;
        var msg = commitMessage ?? "Kod güncellemesi (arabam AI)";
        var existing = Get(conversationId);
        var allPaths = existing.HasValue
            ? existing.Value.Paths.Concat(paths).Distinct().ToList()
            : paths.ToList();
        _store[conversationId] = (allPaths, msg, diffOrSummary ?? existing?.DiffOrSummary);
    }

    public (IReadOnlyList<string> Paths, string CommitMessage, string? DiffOrSummary)? Get(int conversationId)
    {
        return _store.TryGetValue(conversationId, out var v) ? v : null;
    }

    public void Clear(int conversationId)
    {
        _store.TryRemove(conversationId, out _);
    }
}
