using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Application.Common.Models;
using ToyStore.Application.DTOs.BankAccounts;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class BankLookupService : IBankLookupService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BankLookupService> _logger;

    public BankLookupService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<BankLookupService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<List<BankLookupItemDto>>> GetBanksAsync(CancellationToken cancellationToken = default)
    {
        const string CacheKey = "BankLookup_Banks";
        if (_cache.TryGetValue(CacheKey, out List<BankLookupItemDto>? cachedBanks) && cachedBanks != null)
        {
            return Result<List<BankLookupItemDto>>.Success(cachedBanks);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("BankLookup");
            var response = await client.GetAsync("bank/list", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<List<BankLookupItemDto>>.Failure("BANK_LOOKUP_ERROR", $"Failed to fetch bank list from BankLookup. Status: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            List<BankLookupItemDto>? banks = null;

            try
            {
                using var doc = JsonDocument.Parse(json);

                // api.banklookup.net always returns: { "code": 200, "data": [...], "msg": "..." }
                if (doc.RootElement.TryGetProperty("data", out var dataProp)
                    && dataProp.ValueKind == JsonValueKind.Array)
                {
                    banks = JsonSerializer.Deserialize<List<BankLookupItemDto>>(
                        dataProp.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    _logger.LogError("BankLookup: unexpected response format — 'data' array missing. Raw: {Raw}", json[..Math.Min(json.Length, 300)]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BankLookup: failed to parse bank list response. Raw: {Raw}", json[..Math.Min(json.Length, 300)]);
            }

            if (banks == null)
            {
                return Result<List<BankLookupItemDto>>.Failure("BANK_LOOKUP_ERROR", "Failed to parse bank list response.");
            }

            _cache.Set(CacheKey, banks, TimeSpan.FromHours(24));
            return Result<List<BankLookupItemDto>>.Success(banks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching supported bank list");
            return Result<List<BankLookupItemDto>>.Failure("BANK_LOOKUP_ERROR", $"Internal bank lookup error: {ex.Message}");
        }
    }

    public async Task<Result<string>> LookupOwnerNameAsync(string bankCode, string accountNumber, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["BankLookup:ApiKey"];
        var apiSecret = _configuration["BankLookup:ApiSecret"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            _logger.LogError("BankLookup ApiKey or ApiSecret is missing in configuration.");
            return Result<string>.Failure("CONFIGURATION_ERROR", "BankLookup API credentials are not configured.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("BankLookup");
            var request = new HttpRequestMessage(HttpMethod.Post, "")
            {
                Content = JsonContent.Create(new { bank = bankCode, account = accountNumber })
            };
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("x-api-secret", apiSecret);

            var response = await client.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            {
                return Result<string>.UnprocessableEntity("The account number does not exist, or the bank does not support account lookup.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result<string>.Failure("BANK_LOOKUP_ERROR", $"Bank owner lookup failed with HTTP {(int)response.StatusCode}. Response: {responseText}");
            }

            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("data", out JsonElement dataProp) && dataProp.TryGetProperty("ownerName", out JsonElement nameProp))
            {
                var ownerName = nameProp.GetString();
                if (!string.IsNullOrWhiteSpace(ownerName))
                {
                    return Result<string>.Success(ownerName);
                }
            }

            return Result<string>.Failure("BANK_LOOKUP_ERROR", "Could not retrieve ownerName from API response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while looking up owner name for bank: {Bank}, account: {Account}", bankCode, accountNumber);
            return Result<string>.Failure("BANK_LOOKUP_ERROR", $"Internal owner lookup error: {ex.Message}");
        }
    }
}
