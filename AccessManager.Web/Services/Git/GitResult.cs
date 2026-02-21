namespace AccessManager.UI.Services.Git;

public sealed class GitResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static GitResult Ok(string message = "Push başarılı. Deploy süreci tetiklendi.") =>
        new() { Success = true, Message = message };

    public static GitResult Fail(string message) =>
        new() { Success = false, Message = message };
}
