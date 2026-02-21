namespace AccessManager.UI.Services.CodeModification;

public interface ICodeModificationService
{
    /// <summary>
    /// Tek bir dosyaya unified diff uygular. Path repo köküne göre relative olmalı.
    /// </summary>
    Task<ApplyDiffResult> ApplyDiffAsync(string relativePath, string unifiedDiff, CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden fazla dosyaya diff uygular, sonra commit + push yapar.
    /// </summary>
    Task<CodeModificationResult> ApplyDiffsAndPushAsync(
        IReadOnlyList<FileDiffInput> files,
        string commitMessage,
        CancellationToken cancellationToken = default);
}

public sealed class FileDiffInput
{
    public required string Path { get; init; }
    public required string Diff { get; init; }
}

public sealed class ApplyDiffResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class CodeModificationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}
