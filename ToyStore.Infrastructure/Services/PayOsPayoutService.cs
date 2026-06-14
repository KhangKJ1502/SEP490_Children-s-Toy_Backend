using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

public class PayOsPayoutService : IPayOsPayoutService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PayOsOptions _opts;
    private readonly ILogger<PayOsPayoutService> _logger;

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public PayOsPayoutService(
        IHttpClientFactory httpClientFactory,
        IOptions<PayOsOptions> opts,
        ILogger<PayOsPayoutService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<PayOsPayoutResult> CreatePayoutAsync(
        string referenceId,
        decimal amount,
        string toBankBin,
        string toAccountNumber,
        string toAccountName,
        string description,
        CancellationToken ct = default)
    {
        var body = new
        {
            referenceId,
            amount = (long)amount,
            toBin = toBankBin,
            toAccountNumber,
            description,
            category = new[] { "withdrawal" },
        };

        var bodyJson = JsonSerializer.Serialize(body, _json);

        // PayOS payout signature: HMAC-SHA256 of sorted key=value pairs (same convention as webhook).
        // Array fields (category) are excluded from the signature per PayOS spec.
        var signature = ComputePayoutSignature(
            referenceId,
            (long)amount,
            toBankBin,
            toAccountNumber,
            description);

        int maxRetries = 3;
        int[] backoffSeconds = { 2, 4, 8 };
        // NOTE: We intentionally do NOT pass the request-scoped CancellationToken (ct)
        // into the outbound PayOS HTTP call. Once funds are locked, this call MUST complete
        // regardless of whether the HTTP request context is still alive. Using the request ct
        // causes TaskCanceledException when the ASP.NET response is sent before the PayOS
        // TLS round-trip finishes (SocketError 995: I/O aborted). Each attempt gets its own
        // 30-second hard timeout. The inter-attempt delay still respects the caller's ct.
        const int httpTimeoutSeconds = 30;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(TimeSpan.FromSeconds(backoffSeconds[attempt - 1]), ct);

            try
            {
                using var attemptCts = new CancellationTokenSource(TimeSpan.FromSeconds(httpTimeoutSeconds));
                using var client = _httpClientFactory.CreateClient("PayOsPayout");
                using var request = new HttpRequestMessage(HttpMethod.Post, "v1/payouts");

                request.Headers.Add("x-api-key", _opts.PayoutApiKey);
                request.Headers.Add("x-client-id", _opts.PayoutClientId);
                request.Headers.Add("x-idempotency-key", referenceId);
                request.Headers.Add("x-signature", signature);
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request, attemptCts.Token);
                var rawResponse = await response.Content.ReadAsStringAsync(attemptCts.Token);

                _logger.LogInformation(
                    "PayOS payout attempt {Attempt}: POST {BaseUrl}v1/payouts → HTTP {StatusCode} | ref={Ref} | body={Body}",
                    attempt + 1,
                    client.BaseAddress?.ToString() ?? "???",
                    (int)response.StatusCode,
                    referenceId,
                    rawResponse[..Math.Min(rawResponse.Length, 500)]);

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(rawResponse);
                    var root = doc.RootElement;

                    var code = root.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;
                    if (code != "00")
                    {
                        var desc = root.TryGetProperty("desc", out var descProp) ? descProp.GetString() : "Unknown PayOS error";
                        _logger.LogWarning("PayOS payout failed at application level with code {Code}: {Desc} for {Ref}", code, desc, referenceId);
                        return new PayOsPayoutResult(false, null, null, rawResponse, $"PayOS error {code}: {desc}");
                    }

                    // Guard: PayOS may return "data": null on async/queued payouts.
                    // TryGetProperty returns true for an existing null key but throws
                    // InvalidOperationException if you call TryGetProperty on a Null element.
                    var payoutId = root.TryGetProperty("data", out var data)
                                   && data.ValueKind == JsonValueKind.Object
                        ? data.TryGetProperty("id", out var idProp) ? idProp.GetString() : null
                        : null;

                    // transactions[0].id — PayOS returns the bank transaction id in the nested array
                    string? transactionId = null;
                    if (root.TryGetProperty("data", out var d2)
                        && d2.ValueKind == JsonValueKind.Object
                        && d2.TryGetProperty("transactions", out var txns)
                        && txns.ValueKind == JsonValueKind.Array
                        && txns.GetArrayLength() > 0)
                    {
                        transactionId = txns[0].TryGetProperty("id", out var tidProp)
                            ? tidProp.GetString()
                            : null;
                    }

                    _logger.LogInformation("PayOS payout created: ref={Ref} payoutId={PayoutId}", referenceId, payoutId);
                    return new PayOsPayoutResult(true, payoutId, transactionId, rawResponse, null);
                }

                // Retry on rate-limit and server errors
                if ((int)response.StatusCode == 429 || response.StatusCode >= HttpStatusCode.InternalServerError)
                {
                    _logger.LogWarning("PayOS payout attempt {Attempt} failed with {StatusCode} for {Ref}", attempt + 1, response.StatusCode, referenceId);
                    continue;
                }

                // Non-retryable client error
                _logger.LogError("PayOS payout non-retryable error {StatusCode} for {Ref}: {Body}", response.StatusCode, referenceId, rawResponse);
                return new PayOsPayoutResult(false, null, null, rawResponse, $"PayOS error {(int)response.StatusCode}: {rawResponse}");
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                _logger.LogWarning(ex, "PayOS payout attempt {Attempt} threw exception for {Ref}", attempt + 1, referenceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS payout failed after {MaxRetries} attempts for {Ref}", maxRetries, referenceId);
                return new PayOsPayoutResult(false, null, null, null, ex.Message);
            }
        }

        return new PayOsPayoutResult(false, null, null, null, "PayOS payout failed after maximum retries");
    }

    public async Task<PayOsPayoutDetailResult> GetPayoutAsync(string payoutId, CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("PayOsPayout");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/payouts/{payoutId}");

            request.Headers.Add("x-api-key", _opts.PayoutApiKey);
            request.Headers.Add("x-client-id", _opts.PayoutClientId);

            var response = await client.SendAsync(request, ct);
            var rawResponse = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PayOS GetPayout returned {StatusCode} for payoutId={PayoutId}: {Body}",
                    response.StatusCode, payoutId, rawResponse);
                return new PayOsPayoutDetailResult(false, null, null, null, rawResponse,
                    $"PayOS GetPayout error {(int)response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(rawResponse);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var data))
                return new PayOsPayoutDetailResult(false, null, null, null, rawResponse, "PayOS response missing 'data' field");

            var approvalState = data.TryGetProperty("approvalState", out var asp) ? asp.GetString() : null;

            string? transactionState = null;
            string? transactionId = null;
            if (data.TryGetProperty("transactions", out var txns)
                && txns.ValueKind == JsonValueKind.Array
                && txns.GetArrayLength() > 0)
            {
                transactionId   = txns[0].TryGetProperty("id",    out var tidProp)   ? tidProp.GetString()   : null;
                transactionState = txns[0].TryGetProperty("state", out var stateProp) ? stateProp.GetString() : null;
            }

            _logger.LogInformation("PayOS GetPayout payoutId={PayoutId} approvalState={State}", payoutId, approvalState);
            return new PayOsPayoutDetailResult(true, approvalState, transactionState, transactionId, rawResponse, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS GetPayout threw exception for payoutId={PayoutId}", payoutId);
            return new PayOsPayoutDetailResult(false, null, null, null, null, ex.Message);
        }
    }

    private string ComputePayoutSignature(
        string referenceId,
        long amount,
        string toBin,
        string toAccountNumber,
        string description)
    {
        // PayOS SDK uses Uri.EscapeDataString on both key AND value before HMAC (EncodeUri=true default).
        // Array fields like category are serialized to JSON string first (e.g. ["withdrawal"])
        // and then escaped.
        // Ref: payOSHQ/payos-lib-dotnet Payouts signature specifications
        var categoryJson = "[\"withdrawal\"]";

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["amount"]          = amount.ToString(),
            ["category"]        = categoryJson,
            ["description"]     = description,
            ["referenceId"]     = referenceId,
            ["toAccountNumber"] = toAccountNumber,
            ["toBin"]           = toBin,
        };

        var dataToSign = string.Join("&", fields.Select(
            kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var key  = Encoding.UTF8.GetBytes(_opts.ChecksumKey);
        var data = Encoding.UTF8.GetBytes(dataToSign);
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(data)).ToLowerInvariant();
    }
}
