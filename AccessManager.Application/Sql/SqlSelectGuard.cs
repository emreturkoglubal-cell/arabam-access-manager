using System.Text.RegularExpressions;

namespace AccessManager.Application.Sql;

/// <summary>
/// Yalnızca tek SELECT (veya WITH … SELECT) ifadesine izin verir; LIMIT üst sınırı ve bloklu anahtar kelimeler uygular.
/// </summary>
public static class SqlSelectGuard
{
    public const int MaxRows = 1000;
    public const int MaxSqlLength = 200_000;

    private static readonly Regex LimitTailRx = new(
        @"\blimit\s+(\d+)\s*;?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    /// <summary>Bilinen yazma veya tehlikeli PostgreSQL kalıpları (kelime sınırları ile).</summary>
    private static readonly Regex ForbiddenRx = new(
        @"\b(INSERT|UPDATE|DELETE|MERGE|DROP|TRUNCATE|ALTER|CREATE|GRANT|REVOKE|COPY|EXECUTE|EXEC|CALL|DO|LISTEN|NOTIFY|VACUUM|ANALYZE|CLUSTER|REINDEX|REFRESH\s+MATERIALIZED|PREPARE|DEALLOCATE)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex IntoRx = new(@"\bINTO\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ForUpdateRx = new(@"\bFOR\s+(UPDATE|SHARE)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static SqlGuardResult ValidateAndNormalize(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return SqlGuardResult.Fail("SQL boş olamaz.");

        var raw = sql.Trim();
        if (raw.Length > MaxSqlLength)
            return SqlGuardResult.Fail($"SQL en fazla {MaxSqlLength} karakter olabilir.");

        // Tek ifade: içteki stringlerde ; olabilir — şirket içi araçta basit kural: tek ';' yok veya yalnızca sonda.
        raw = raw.TrimEnd().TrimEnd(';').TrimEnd();
        if (raw.Contains(';'))
            return SqlGuardResult.Fail("Birden fazla SQL ifadesi kullanılamaz (; ile ayırma).");

        if (ForbiddenRx.IsMatch(raw))
            return SqlGuardResult.Fail("Bu SQL türüne izin verilmiyor (yalnızca okuma SELECT).");

        if (IntoRx.IsMatch(raw))
            return SqlGuardResult.Fail("INTO içeren ifadelere izin verilmiyor.");

        if (ForUpdateRx.IsMatch(raw))
            return SqlGuardResult.Fail("FOR UPDATE / FOR SHARE kullanılamaz.");

        // WITH ... veya SELECT ile başlamalı (veya yorum sonrası — basit: başlangıç kontrolü)
        if (!raw.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            return SqlGuardResult.Fail("Yalnızca SELECT veya WITH … SELECT sorgularına izin verilir.");

        var normalized = ApplyLimitClamp(raw);
        return SqlGuardResult.Succeed(normalized);
    }

    private static string ApplyLimitClamp(string sql)
    {
        var m = LimitTailRx.Match(sql.TrimEnd());
        if (m.Success && int.TryParse(m.Groups[1].Value, out var lim))
        {
            var clamped = Math.Min(lim, MaxRows);
            return LimitTailRx.Replace(sql.TrimEnd(), $"LIMIT {clamped}", 1);
        }

        return sql.TrimEnd() + " LIMIT " + MaxRows;
    }

    public readonly struct SqlGuardResult
    {
        public bool IsValid { get; init; }
        public string? ErrorMessage { get; init; }
        public string? NormalizedSql { get; init; }

        public static SqlGuardResult Fail(string msg) => new() { IsValid = false, ErrorMessage = msg };

        public static SqlGuardResult Succeed(string normalized) => new() { IsValid = true, NormalizedSql = normalized };
    }
}
