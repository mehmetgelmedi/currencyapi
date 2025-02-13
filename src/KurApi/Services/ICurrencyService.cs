using System.Collections.Generic;
using System.Threading.Tasks;
using KurApi.Models;

namespace KurApi.Services;

public interface ICurrencyService
{
    Task<IEnumerable<Currency>> GetAllCurrenciesAsync();
    Task<Currency> GetCurrencyByCodeAsync(string code);
    Task<ExchangeRateInfo> GetExchangeRateAsync(string fromCode, string toCode);
    Task UpdateCurrenciesFromTCMBAsync();
} 