using AccessManager.Application.Sql;

namespace AccessManager.Application.Interfaces;

/// <summary>
/// Önceden doğrulanmış SELECT metnini PostgreSQL üzerinde çalıştırır; sonucu metin olarak döner.
/// </summary>
public interface IReadOnlySqlQueryService
{
    /// <param name="validatedSql"><see cref="SqlSelectGuard"/> ile onaylanmış tam metin.</param>
    Task<string> ExecuteSelectAsync(string validatedSql, CancellationToken cancellationToken = default);
}
