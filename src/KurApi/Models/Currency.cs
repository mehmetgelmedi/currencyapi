using System;

namespace KurApi.Models;

public class Currency
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal ForexBuying { get; set; }
    public decimal ForexSelling { get; set; }
    public decimal BanknoteBuying { get; set; }
    public decimal BanknoteSelling { get; set; }
    public DateTime LastUpdated { get; set; }
} 