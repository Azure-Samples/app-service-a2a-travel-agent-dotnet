using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace TravelAgent.Core.Plugins;

/// <summary>
/// Currency exchange plugin using Frankfurter API.
/// Equivalent to Python's CurrencyPlugin class.
/// </summary>
public class CurrencyPlugin
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CurrencyPlugin>? _logger;

    public CurrencyPlugin(HttpClient httpClient, ILogger<CurrencyPlugin>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get current exchange rate between two currencies.
    /// Equivalent to Python's get_exchange_rate method.
    /// </summary>
    /// <param name="fromCurrency">Source currency code (e.g., USD)</param>
    /// <param name="toCurrency">Target currency code (e.g., EUR)</param>
    /// <returns>Exchange rate information</returns>
    [KernelFunction, Description("Get current exchange rate between two currencies using Frankfurter API")]
    public async Task<string> GetExchangeRateAsync(
        [Description("Source currency code (e.g., USD, EUR, GBP)")] string fromCurrency,
        [Description("Target currency code (e.g., USD, EUR, GBP)")] string toCurrency)
    {
        try
        {
            var url = $"https://api.frankfurter.app/latest?from={fromCurrency.ToUpper()}&to={toCurrency.ToUpper()}";
            _logger?.LogInformation("Fetching exchange rate from {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            if (data.TryGetProperty("rates", out var rates) &&
                rates.TryGetProperty(toCurrency.ToUpper(), out var rate))
            {
                return $"1 {fromCurrency.ToUpper()} = {rate.GetDecimal():F4} {toCurrency.ToUpper()}";
            }

            return $"Unable to get exchange rate from {fromCurrency} to {toCurrency}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching exchange rate from {From} to {To}", fromCurrency, toCurrency);
            return $"Error fetching exchange rate: {ex.Message}";
        }
    }

    /// <summary>
    /// Convert amount from one currency to another.
    /// Equivalent to Python's convert_currency method.
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="toCurrency">Target currency code</param>
    /// <returns>Converted amount information</returns>
    [KernelFunction, Description("Convert amount from one currency to another using current exchange rates")]
    public async Task<string> ConvertCurrencyAsync(
        [Description("Amount to convert")] decimal amount,
        [Description("Source currency code (e.g., USD, EUR, GBP)")] string fromCurrency,
        [Description("Target currency code (e.g., USD, EUR, GBP)")] string toCurrency)
    {
        try
        {
            var url = $"https://api.frankfurter.app/latest?from={fromCurrency.ToUpper()}&to={toCurrency.ToUpper()}&amount={amount}";
            _logger?.LogInformation("Converting {Amount} {From} to {To}", amount, fromCurrency, toCurrency);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);

            if (data.TryGetProperty("rates", out var rates) &&
                rates.TryGetProperty(toCurrency.ToUpper(), out var convertedAmount))
            {
                return $"{amount} {fromCurrency.ToUpper()} = {convertedAmount.GetDecimal():F2} {toCurrency.ToUpper()}";
            }

            return $"Unable to convert {amount} {fromCurrency} to {toCurrency}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error converting currency from {From} to {To}", fromCurrency, toCurrency);
            return $"Error converting currency: {ex.Message}";
        }
    }
}