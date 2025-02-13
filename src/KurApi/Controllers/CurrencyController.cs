using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KurApi.Models;
using KurApi.Services;

namespace KurApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(
            ICurrencyService currencyService,
            ILogger<CurrencyController> logger)
        {
            _currencyService = currencyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCurrencies()
        {
            try
            {
                var currencies = await _currencyService.GetAllCurrenciesAsync();
                return Ok(currencies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting currencies");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetCurrency(string code)
        {
            try
            {
                var currency = await _currencyService.GetCurrencyByCodeAsync(code);
                if (currency == null)
                    return NotFound($"Currency with code {code} not found");

                return Ok(currency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting currency for code: {code}", code);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("exchange-rate")]
        public async Task<IActionResult> GetExchangeRate([FromQuery] string from, [FromQuery] string to)
        {
            try
            {
                var (rate, lastUpdated) = await _currencyService.GetExchangeRateAsync(from, to);
                return Ok(new { rate, lastUpdated });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting exchange rate from {from} to {to}", from, to);
                return StatusCode(500, "Internal server error");
            }
        }
    }
} 