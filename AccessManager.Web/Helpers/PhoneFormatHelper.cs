namespace AccessManager.UI.Helpers;

/// <summary>
/// Türkiye telefon numarası: +90 (XXX) XXX XX XX formatı.
/// </summary>
public static class PhoneFormatHelper
{
    public const string FormatPattern = "+90 (XXX) XXX XX XX";
    public const int RequiredDigits = 10;

    /// <summary>Değerden sadece rakamları alır (en fazla 10).</summary>
    public static string GetDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length > RequiredDigits ? digits[..RequiredDigits] : digits;
    }

    /// <summary>10 rakamı +90 (XXX) XXX XX XX formatına çevirir.</summary>
    public static string FormatFromDigits(string digits)
    {
        if (string.IsNullOrEmpty(digits) || digits.Length != RequiredDigits)
            return string.Empty;
        return $"+90 ({digits[0]}{digits[1]}{digits[2]}) {digits[3]}{digits[4]}{digits[5]} {digits[6]}{digits[7]} {digits[8]}{digits[9]}";
    }

    /// <summary>Veritabanındaki veya kullanıcı girişindeki değeri gösterim formatına çevirir. Boşsa null döner.</summary>
    public static string? Format(string? phone)
    {
        var digits = GetDigits(phone);
        if (digits.Length != RequiredDigits) return null;
        return FormatFromDigits(digits);
    }

    /// <summary>Telefonu normalize eder: 10 rakam varsa formatlanmış string, yoksa null. Kayıt için kullanılır.</summary>
    public static string? NormalizeForSave(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = GetDigits(phone);
        if (digits.Length != RequiredDigits) return null;
        return FormatFromDigits(digits);
    }

    /// <summary>Doğrular: boş geçilebilir; doluysa tam 10 rakam olmalı.</summary>
    public static (bool Valid, string? ErrorMessage) Validate(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return (true, null);
        var digits = GetDigits(phone);
        if (digits.Length != RequiredDigits)
            return (false, $"Telefon numarası tam olarak {RequiredDigits} rakam içermelidir. Format: {FormatPattern}");
        return (true, null);
    }
}
