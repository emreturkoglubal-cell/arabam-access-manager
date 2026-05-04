using AccessManager.Application.Interfaces;
using AccessManager.Application.Sql;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AccessManager.Infrastructure.Services;

public sealed class ReadOnlySqlQueryService : IReadOnlySqlQueryService
{
    private const int CommandTimeoutSeconds = 30;
    private const int MaxOutputChars = 100_000;

    /// <summary>Boş sonuçta modelin aynı sorguyu tekrar propose etmesini engellemek için araç çıktısına eklenir.</summary>
    private static string EmptyResultAssistantDirective =>
        "\n\n---\n**Sistem (yalnızca asistan):** Veritabanı sorguyu başarıyla çalıştırdı; sonuç kümesi **boş** (0 satır). " +
        "Kullanıcıya tek yanıtta kısaca bildir (ör. «Bu kriterlere uyan kayıt bulunamadı» / «Listede eşleşen satır yok»). " +
        "**Yapma:** propose_sql ile aynı veya benzer sorguyu yeniden önerme, tekrar onay isteme, execute_pending_sql'i ikinci kez çağırma. Bu SQL turu tamamlanmıştır; gerekirse kullanıcıdan yeni bir soru bekle.\n";

    private readonly string? _connectionString;

    public ReadOnlySqlQueryService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")?.Trim();
    }

    public async Task<string> ExecuteSelectAsync(string validatedSql, CancellationToken cancellationToken = default)
    {
        var guard = SqlSelectGuard.ValidateAndNormalize(validatedSql);
        if (!guard.IsValid || string.IsNullOrEmpty(guard.NormalizedSql))
            return "HATA: SQL doğrulanamadı: " + (guard.ErrorMessage ?? "bilinmeyen");

        if (string.IsNullOrEmpty(_connectionString))
            return "HATA: ConnectionStrings:DefaultConnection yapılandırılmamış.";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using (var setCmd = new NpgsqlCommand("SET LOCAL statement_timeout = '30s'", conn))
        {
            setCmd.CommandTimeout = CommandTimeoutSeconds;
            await setCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var cmd = new NpgsqlCommand(guard.NormalizedSql, conn)
        {
            CommandTimeout = CommandTimeoutSeconds
        };

        await using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("**Sonuç** (en fazla ").Append(SqlSelectGuard.MaxRows).AppendLine(" satır):\n");

        if (!reader.HasRows)
        {
            sb.AppendLine("*Sorgu başarıyla çalıştı; eşleşen satır yok (0 kayıt). Bu bir hataya değildir — filtre/join kriterlerine uyan veri bulunamadı.*");
            sb.Append(EmptyResultAssistantDirective);
            return sb.ToString();
        }

        var fieldCount = reader.FieldCount;
        var colNames = new string[fieldCount];
        for (var i = 0; i < fieldCount; i++)
            colNames[i] = reader.GetName(i);

        sb.Append('|');
        foreach (var cn in colNames)
            sb.Append(' ').Append(EscapeMdCell(cn)).Append(" |");
        sb.AppendLine();
        sb.Append('|');
        foreach (var _ in colNames)
            sb.Append(" --- |");
        sb.AppendLine();

        var rowCount = 0;
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (rowCount >= SqlSelectGuard.MaxRows)
                break;

            rowCount++;
            sb.Append('|');
            for (var i = 0; i < fieldCount; i++)
            {
                var v = reader.IsDBNull(i) ? "" : reader.GetValue(i)?.ToString() ?? "";
                sb.Append(' ').Append(EscapeMdCell(v)).Append(" |");
            }

            sb.AppendLine();

            if (sb.Length >= MaxOutputChars)
            {
                sb.AppendLine("\n… (çıktı uzunluğu sınırına ulaşıldı, kesildi.)");
                break;
            }
        }

        sb.AppendLine();
        if (rowCount == 0)
        {
            sb.AppendLine("*Sorgu başarıyla çalıştı; sonuç kümesinde satır yok (0 kayıt).*");
            sb.Append(EmptyResultAssistantDirective);
        }
        else
            sb.Append('*').Append(rowCount).AppendLine(" satır gösterildi.*");
        return sb.ToString();
    }

    private static string EscapeMdCell(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
