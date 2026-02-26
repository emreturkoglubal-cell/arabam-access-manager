namespace AccessManager.UI.Helpers;

/// <summary>Satın alma ücreti para birimi: TRY (TL), USD, EUR.</summary>
public static class CurrencyHelper
{
    public const string Try = "TRY";
    public const string Usd = "USD";
    public const string Eur = "EUR";

    /// <summary>Para birimi sembolü (gösterim için).</summary>
    public static string GetSymbol(string? currency)
    {
        return currency switch
        {
            Try => "₺",
            Usd => "$",
            Eur => "€",
            _ => "₺"
        };
    }

    /// <summary>Kısa etiket (form dropdown için): "TL (₺)", "USD ($)", "EUR (€)".</summary>
    public static string GetLabel(string? currency)
    {
        return currency switch
        {
            Try => "TL (₺)",
            Usd => "USD ($)",
            Eur => "EUR (€)",
            _ => "TL (₺)"
        };
    }

    public static IReadOnlyList<(string Code, string Label)> GetCurrencies()
    {
        return new List<(string, string)>
        {
            (Try, GetLabel(Try)),
            (Usd, GetLabel(Usd)),
            (Eur, GetLabel(Eur))
        };
    }
}
