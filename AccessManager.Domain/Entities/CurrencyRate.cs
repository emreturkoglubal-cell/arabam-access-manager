namespace AccessManager.Domain.Entities;

/// <summary>
/// Para birimi kuru (baz: USD). amount_usd = amount_in_currency * RateToUsd
/// </summary>
public class CurrencyRate
{
    public string Code { get; set; } = string.Empty;
    public decimal RateToUsd { get; set; }
}
