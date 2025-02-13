using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using KurApi.Models;
using System.Xml.Linq;
using System.Xml;
using Microsoft.Extensions.Configuration;

namespace KurApi.Services;

public class CurrencyService : ICurrencyService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    public CurrencyService(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<IEnumerable<Currency>> GetAllCurrenciesAsync()
    {
        var cacheKey = _configuration["CurrencySettings:CacheKey"];
        var cacheDuration = _configuration.GetValue<int>("CurrencySettings:CacheDurationInMinutes");

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheDuration);
            return await FetchCurrenciesFromTCMB();
        });
    }

    public async Task<Currency> GetCurrencyByCodeAsync(string code)
    {
        var currencies = await GetAllCurrenciesAsync();
        return currencies.FirstOrDefault(c => c.Code == code.ToUpper());
    }

    public async Task<ExchangeRateInfo> GetExchangeRateAsync(string fromCode, string toCode)
    {
        var currencies = await GetAllCurrenciesAsync();
        var fromCurrency = currencies.FirstOrDefault(c => c.Code == fromCode.ToUpper());
        var toCurrency = currencies.FirstOrDefault(c => c.Code == toCode.ToUpper());

        if (fromCurrency == null || toCurrency == null)
            throw new ArgumentException("Currency not found");

        var rate = toCurrency.ForexSelling / fromCurrency.ForexSelling;
        return new ExchangeRateInfo(rate, DateTime.Now);
    }

    private async Task UpdateIfNeededAsync()
    {
        var cacheKey = _configuration["CurrencySettings:CacheKey"];
        var cacheDuration = _configuration.GetValue<int>("CurrencySettings:CacheDurationInMinutes");

        if (!_cache.TryGetValue(cacheKey, out List<Currency> cachedCurrencies))
        {
            await FetchCurrenciesFromTCMB();
        }
        else
        {
            if (cachedCurrencies != null && !IsUpdateNeeded(cachedCurrencies, cacheDuration))
            {
                return;
            }
        }

        await UpdateCurrenciesFromTCMBAsync();
        _cache.Set(cacheKey, await FetchCurrenciesFromTCMB(), TimeSpan.FromMinutes(cacheDuration));
    }

    private bool IsUpdateNeeded(List<Currency> currencies, int cacheDurationMinutes)
    {
        if (!currencies.Any()) return true;
        
        var lastUpdate = currencies.First().LastUpdated;
        return DateTime.Now.Subtract(lastUpdate).TotalMinutes > cacheDurationMinutes;
    }

    private async Task<List<Currency>> FetchCurrenciesFromTCMB()
    {
        var currencies = new List<Currency>();
        using var client = new HttpClient();
        var tcmbUrl = _configuration["CurrencySettings:TcmbUrl"];

        try
        {
            var response = await client.GetStringAsync(tcmbUrl);
            var doc = new XmlDocument();
            doc.LoadXml(response);

            var nodes = doc.SelectNodes("//Currency");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    var currency = new Currency
                    {
                        Code = node.Attributes?["CurrencyCode"]?.Value ?? "",
                        Name = node.SelectSingleNode("CurrencyName")?.InnerText ?? "",
                        ForexBuying = ParseDecimal(node.SelectSingleNode("ForexBuying")?.InnerText),
                        ForexSelling = ParseDecimal(node.SelectSingleNode("ForexSelling")?.InnerText),
                        BanknoteBuying = ParseDecimal(node.SelectSingleNode("BanknoteBuying")?.InnerText),
                        BanknoteSelling = ParseDecimal(node.SelectSingleNode("BanknoteSelling")?.InnerText),
                        LastUpdated = DateTime.Now
                    };
                    currencies.Add(currency);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching currencies: {ex.Message}");
        }

        return currencies;
    }

    private static decimal ParseDecimal(string? value)
    {
        return decimal.TryParse(value, out var result) ? result : 0;
    }

    public async Task UpdateCurrenciesFromTCMBAsync()
    {
        try
        {
            var tcmbUrl = _configuration["CurrencySettings:TcmbUrl"];
            var cacheKey = _configuration["CurrencySettings:CacheKey"];
            var cacheDuration = _configuration.GetValue<int>("CurrencySettings:CacheDurationInMinutes");

            using var client = new HttpClient();
            var response = await client.GetStringAsync(tcmbUrl);
            var xml = XDocument.Parse(response);
            var currencies = new List<Currency>();

            foreach (var currency in xml.Descendants("Currency"))
            {
                currencies.Add(new Currency
                {
                    Code = currency.Attribute("CurrencyCode").Value,
                    Name = currency.Element("CurrencyName").Value,
                    ForexBuying = decimal.Parse(currency.Element("ForexBuying").Value),
                    ForexSelling = decimal.Parse(currency.Element("ForexSelling").Value),
                    BanknoteBuying = decimal.Parse(currency.Element("BanknoteBuying").Value),
                    BanknoteSelling = decimal.Parse(currency.Element("BanknoteSelling").Value),
                    LastUpdated = DateTime.Now
                });
            }

            // TRY'yi de ekle
            currencies.Add(new Currency
            {
                Code = "TRY",
                Name = "TÜRK LİRASI",
                ForexBuying = 1,
                ForexSelling = 1,
                BanknoteBuying = 1,
                BanknoteSelling = 1,
                LastUpdated = DateTime.Now
            });

            // Cache'i güncelle
            _cache.Set(cacheKey, currencies, TimeSpan.FromMinutes(cacheDuration));
        }
        catch (Exception ex)
        {
            throw;
        }
    }
} 