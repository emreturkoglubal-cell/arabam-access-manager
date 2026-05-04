namespace AccessManager.UI.Services.Agent;

/// <summary>
/// AI agent'ın kullandığı araçları çalıştırır: read_file, write_file, apply_diff, run_build, git_commit_and_push, confirm_and_push, create_pr, propose_sql, execute_pending_sql.
/// </summary>
public interface IAgentTools
{
    string ReadFile(string relativePath);
    Task<string> WriteFileAsync(string relativePath, string content, int? conversationId = null, CancellationToken cancellationToken = default);
    Task<string> ApplyDiffAsync(string relativePath, string unifiedDiff, int? conversationId = null, CancellationToken cancellationToken = default);
    /// <summary>Projeyi derler (dotnet build). Push öncesi build hatası varsa kullanıcıya bildirmek için kullanılır.</summary>
    Task<string> RunBuildAsync(CancellationToken cancellationToken = default);
    Task<string> GitCommitAndPushAsync(string commitMessage, IReadOnlyList<string> relativePaths, CancellationToken cancellationToken = default);
    /// <summary>Onay bekleyen değişiklikleri build alıp başarılıysa commit edip main'e push eder. Build hata verirse push etmez.</summary>
    Task<string> ConfirmPushAsync(int conversationId, CancellationToken cancellationToken = default);
    /// <summary>Onay bekleyen değişiklikleri yeni branch'e commit edip push eder; kullanıcı PR açar. Pushlamak yerine PR istendiğinde kullan.</summary>
    Task<string> CreatePrAsync(int conversationId, CancellationToken cancellationToken = default);

    /// <summary>SELECT doğrular, normalize eder ve konuşmaya bekleyen SQL olarak kaydeder.</summary>
    Task<string> ProposeSqlAsync(string sql, int conversationId, CancellationToken cancellationToken = default);

    /// <summary>Bekleyen onaylı SELECT'i çalıştırır ve store'u temizler.</summary>
    Task<string> ExecutePendingSqlAsync(int conversationId, CancellationToken cancellationToken = default);
}
