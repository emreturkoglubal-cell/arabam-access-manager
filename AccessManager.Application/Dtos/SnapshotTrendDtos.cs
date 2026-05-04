namespace AccessManager.Application.Dtos;

public class MonthTotalUsdPair
{
    public string Label { get; set; } = string.Empty;
    public decimal TotalUsd { get; set; }
}

public class SystemMonthCostPoint
{
    public string Label { get; set; } = string.Empty;
    public DateTime MonthStart { get; set; }
    public decimal TotalCostUsd { get; set; }
    public int ActiveAccessCount { get; set; }
}

public class MonthInventoryTotalPair
{
    public string Label { get; set; } = string.Empty;
    public int TotalCount { get; set; }
}
