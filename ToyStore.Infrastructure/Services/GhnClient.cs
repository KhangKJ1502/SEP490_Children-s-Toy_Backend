using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ToyStore.Application.Common.Helpers;
using ToyStore.Application.DTOs.Checkouts;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Options;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Client GHN cho van chuyen.
/// Xu ly tinh phi, leadtime va tao don
/// co retry va timeout theo tung lan goi.
/// Cau hinh doc tu <see cref="GhnOptions"/> (section "GHN")
/// va <see cref="ShopAddressOptions"/> (section "ShopAddress") trong appsettings.
/// </summary>
public sealed class GhnClient : IGhnClient
{
    private const string HttpClientName = "GHN";
    private const int ResponseTruncateLength = 300;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };


    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GhnOptions _ghnOptions;
    private readonly ShopAddressOptions _shopAddressOptions;
    private readonly ILogger<GhnClient> _logger;

    public GhnClient(
        IHttpClientFactory httpClientFactory,
        IOptions<GhnOptions> ghnOptions,
        IOptions<ShopAddressOptions> shopAddressOptions,
        ILogger<GhnClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _ghnOptions = ghnOptions.Value;
        _shopAddressOptions = shopAddressOptions.Value;
        _logger = logger;
    }


    public async Task<Result<FeeResponseDTO>> GetFeeAsync(
        FeeRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["payment_type_id"] = 2, // Match create_order: shop contract rate (not retail)
            ["from_district_id"] = request.FromDistrictId,
            ["from_ward_code"]   = request.FromWardCode,
            ["to_district_id"]   = request.ToDistrictId,
            ["to_ward_code"]     = request.ToWardCode,
            ["insurance_value"]  = Math.Min(RoundToInt(request.InsuranceValue), 5000000),
            ["cod_value"]        = RoundToInt(request.CodValue),
            ["weight"]           = request.Weight,
            ["length"]           = request.Length,
            ["width"]            = request.Width,
            ["height"]           = request.Height,
            ["service_type_id"]  = (request.ServiceTypeId.HasValue && request.ServiceTypeId.Value > 0) ? request.ServiceTypeId.Value : 2
        };

        if (request.Items != null && request.Items.Any())
        {
            payload["items"] = request.Items.Select(i => new
            {
                name = i.Name,
                code = i.Code,
                quantity = i.Quantity,
                price = i.Price,
                length = i.Length,
                width = i.Width,
                height = i.Height,
                weight = i.Weight,
                category = string.IsNullOrWhiteSpace(i.Category) ? null : (object)new { level1 = i.Category }
            }).ToList();
        }

        _logger.LogInformation(
            "[GHN-FEE-REQUEST] to_district={ToDistrict} to_ward={ToWard} weight={Weight}g length={Length} width={Width} height={Height} service_type_id={ServiceTypeId} insurance={Insurance} cod={Cod}",
            request.ToDistrictId, request.ToWardCode,
            request.Weight, request.Length, request.Width, request.Height,
            payload["service_type_id"],
            RoundToInt(request.InsuranceValue), RoundToInt(request.CodValue));

        var feeResult = await PostAsync<GhnFeeData>("v2/shipping-order/fee", payload, "fee", cancellationToken);

        if (feeResult.IsSuccess)
            _logger.LogInformation("[GHN-FEE-RESPONSE] total_fee={Fee}", feeResult.Data!.Total);

        if (!feeResult.IsSuccess)
        {
            _logger.LogError("GetFeeAsync failed. Error: {Error}", feeResult.ErrorMessage);
            return MapFailure<FeeResponseDTO, GhnFeeData>(feeResult);
        }

        return Result<FeeResponseDTO>.Success(new FeeResponseDTO
        {
            Fee = feeResult.Data!.Total
        });
    }

    public async Task<Result<LeadtimeResponseDTO>> GetLeadtimeAsync(
        LeadtimeRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["from_district_id"] = request.FromDistrictId,
            ["from_ward_code"] = request.FromWardCode,
            ["to_district_id"] = request.ToDistrictId,
            ["to_ward_code"] = request.ToWardCode
        };

        payload["service_type_id"] = request.ServiceTypeId ?? 2;


        var leadtimeResult = await PostAsync<GhnLeadtimeData>(
            "v2/shipping-order/leadtime", payload, "leadtime", cancellationToken);

        if (!leadtimeResult.IsSuccess)
            return MapFailure<LeadtimeResponseDTO, GhnLeadtimeData>(leadtimeResult);

        return Result<LeadtimeResponseDTO>.Success(new LeadtimeResponseDTO
        {
            LeadtimeUnix = leadtimeResult.Data!.Leadtime,
            EstimatedDeliveryTime = DateTimeOffset
                .FromUnixTimeSeconds(leadtimeResult.Data.Leadtime)
                .UtcDateTime
        });
    }

    // ── Tao don van chuyen ──────────────────────────────────────────────────

    public async Task<Result<ShippingOrderCreateResponseDto>> CreateOrderAsync(
        ShippingOrderCreateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!IsSenderConfigured(_shopAddressOptions))
        {
            return Result<ShippingOrderCreateResponseDto>.Failure(
                "CONFIGURATION_ERROR",
                "ShopAddress configuration is incomplete. Check appsettings 'ShopAddress' section.");
        }

        if (!GhnShippingLimits.TryNormalizeCreateOrderRequest(request, out var normalizeError))
        {
            return Result<ShippingOrderCreateResponseDto>.Failure("GHN_DIMENSION_ERROR", normalizeError!);
        }

        var payload = new Dictionary<string, object>
        {
            ["payment_type_id"] = 2,
            ["note"] = request.Note ?? string.Empty,
            ["required_note"] = request.RequiredNote,
            ["from_name"] = !string.IsNullOrWhiteSpace(request.FromName) ? request.FromName : _shopAddressOptions.Name,
            ["from_phone"] = !string.IsNullOrWhiteSpace(request.FromPhone) ? request.FromPhone : _shopAddressOptions.Phone,
            ["from_address"] = !string.IsNullOrWhiteSpace(request.FromAddress) ? request.FromAddress : _shopAddressOptions.AddressLine,
            ["from_ward_name"] = !string.IsNullOrWhiteSpace(request.FromWardName) ? request.FromWardName : _shopAddressOptions.WardName,
            ["from_district_name"] = !string.IsNullOrWhiteSpace(request.FromDistrictName) ? request.FromDistrictName : _shopAddressOptions.DistrictName,
            ["from_district_id"] = _ghnOptions.FromDistrictId > 0
                ? _ghnOptions.FromDistrictId
                : _shopAddressOptions.DistrictId,
            ["from_ward_code"] = !string.IsNullOrWhiteSpace(_ghnOptions.FromWardCode)
                ? _ghnOptions.FromWardCode
                : _shopAddressOptions.WardCode,
            ["to_name"] = request.ToName,
            ["to_phone"] = request.ToPhone,
            ["to_address"] = request.ToAddress,
            ["to_ward_code"] = request.ToWardCode,
            ["to_district_id"] = request.ToDistrictId,
            ["cod_amount"] = RoundToInt(request.CodAmount),
            ["content"] = $"Order {request.ClientOrderCode}",
            ["weight"] = request.Weight,
            ["length"] = request.Length,
            ["width"] = request.Width,
            ["height"] = request.Height,
            ["insurance_value"] = Math.Min(RoundToInt(request.InsuranceValue), 5000000),
            ["client_order_code"] = request.ClientOrderCode,
            ["service_type_id"] = request.ServiceTypeId > 0 ? request.ServiceTypeId : 2,
            ["items"] = request.Items.Select(x => new
            {
                name = x.Name,
                code = string.IsNullOrWhiteSpace(x.Code) ? null : x.Code,
                quantity = x.Quantity,
                price = RoundToInt(x.Price),
                weight = x.Weight,
                length = x.Length > 0 ? x.Length : (int?)null,
                width = x.Width > 0 ? x.Width : (int?)null,
                height = x.Height > 0 ? x.Height : (int?)null,
                category = string.IsNullOrWhiteSpace(x.Category) ? null : (object)new { level1 = x.Category }
            }).ToList()
        };

        _logger.LogInformation(
            "[GHN-CREATE-REQUEST] shop_id={ShopId} from_district={FromDistrict} from_ward={FromWard} to_district={ToDistrict} to_ward={ToWard} weight={Weight}g length={Length} width={Width} height={Height} service_type_id={ServiceTypeId} insurance={Insurance} cod={Cod}",
            _ghnOptions.ShopId,
            payload["from_district_id"], payload["from_ward_code"],
            request.ToDistrictId, request.ToWardCode,
            request.Weight, request.Length, request.Width, request.Height,
            payload["service_type_id"], RoundToInt(request.InsuranceValue), RoundToInt(request.CodAmount));

        foreach (var item in request.Items)
        {
            _logger.LogInformation(
                "[GHN-CREATE-ITEM] name={Name} qty={Qty} weight={Weight}g length={Length} width={Width} height={Height}",
                item.Name, item.Quantity, item.Weight, item.Length, item.Width, item.Height);
        }

        var createResult = await PostAsync<GhnCreateOrderData>(
            "v2/shipping-order/create", payload, "create_order", cancellationToken);

        if (!createResult.IsSuccess)
            return MapFailure<ShippingOrderCreateResponseDto, GhnCreateOrderData>(createResult);

        if (string.IsNullOrWhiteSpace(createResult.Data!.OrderCode))
        {
            return Result<ShippingOrderCreateResponseDto>.Failure(
                "GHN_INVALID_RESPONSE",
                "GHN create order did not return an order_code.");
        }

        return Result<ShippingOrderCreateResponseDto>.Success(new ShippingOrderCreateResponseDto
        {
            OrderCode = createResult.Data.OrderCode,
            SortCode = createResult.Data.SortCode,
            ExpectedDeliveryTime = createResult.Data.ExpectedDeliveryTime,
            TotalFee = createResult.Data.TotalFee
        });
    }

    public async Task<Result> CancelOrderAsync(
        string providerOrderCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerOrderCode))
            return Result.BusinessError("GHN order code is required.");

        var payload = new { order_codes = new[] { providerOrderCode } };
        return await PostCommandAsync("v2/switch-status/cancel", payload, "cancel_order", cancellationToken);
    }

    private async Task<Result<TData>> PostAsync<TData>(
        string path,
        object payload,
        string operation,
        CancellationToken cancellationToken)
    {
        var retryCount = Math.Max(1, _ghnOptions.RetryCount);
        var timeoutSeconds = Math.Max(5, _ghnOptions.TimeoutSeconds);

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                using var client = _httpClientFactory.CreateClient(HttpClientName);
                using var response = await client.PostAsJsonAsync(path, payload, JsonOptions, timeoutCts.Token);
                var responseText = await response.Content.ReadAsStringAsync(timeoutCts.Token);

                // ── HTTP khong thanh cong (non-2xx) ─────────────────────────
                if (!response.IsSuccessStatusCode)
                {
                    if (IsTransient(response.StatusCode) && attempt < retryCount)
                    {
                        await BackoffAsync(attempt, timeoutCts.Token);
                        continue;
                    }
                    return Result<TData>.BusinessError(
                        $"GHN {operation} failed – HTTP {(int)response.StatusCode}: {Truncate(responseText)}");
                }

                // ── Deserialize ─────────────────────────────────────────────
                var body = JsonSerializer.Deserialize<GhnApiResponse<TData>>(responseText, JsonOptions);
                if (body is null)
                {
                    if (attempt < retryCount) { await BackoffAsync(attempt, timeoutCts.Token); continue; }
                    return Result<TData>.Failure("GHN_EMPTY_RESPONSE", $"GHN {operation} returned empty body.");
                }

                // ── GHN business error ─────────────────────────────────────
                if (body.Code != 200)
                {
                    return Result<TData>.BusinessError(
                        $"GHN {operation} business error (code={body.Code}): {body.Message}");
                }

                // ── Thieu data ─────────────────────────────────────────────
                if (body.Data is null)
                {
                    if (attempt < retryCount) { await BackoffAsync(attempt, timeoutCts.Token); continue; }
                    return Result<TData>.Failure("GHN_EMPTY_DATA", $"GHN {operation} returned null data.");
                }

                return Result<TData>.Success(body.Data);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex,
                    "GHN {Operation} timeout – attempt {Attempt}/{RetryCount}",
                    operation, attempt, retryCount);

                if (attempt < retryCount) { await BackoffAsync(attempt, cancellationToken); continue; }
                return Result<TData>.Failure("GHN_TIMEOUT",
                    $"GHN {operation} timed out after {retryCount} attempt(s).");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex,
                    "GHN {Operation} HTTP error – attempt {Attempt}/{RetryCount}",
                    operation, attempt, retryCount);

                if (attempt < retryCount) { await BackoffAsync(attempt, cancellationToken); continue; }
                return Result<TData>.Failure("GHN_HTTP_ERROR",
                    $"GHN {operation} network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GHN {Operation} unexpected error", operation);
                return Result<TData>.Failure("GHN_ERROR",
                    $"Unexpected GHN {operation} error: {ex.Message}");
            }
        }

        return Result<TData>.Failure("GHN_ERROR", $"GHN {operation} failed after {retryCount} retries.");
    }

    private async Task<Result> PostCommandAsync(
        string path,
        object payload,
        string operation,
        CancellationToken cancellationToken)
    {
        var retryCount = Math.Max(1, _ghnOptions.RetryCount);
        var timeoutSeconds = Math.Max(5, _ghnOptions.TimeoutSeconds);

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                using var client = _httpClientFactory.CreateClient(HttpClientName);
                using var response = await client.PostAsJsonAsync(path, payload, JsonOptions, timeoutCts.Token);
                var responseText = await response.Content.ReadAsStringAsync(timeoutCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    if (IsTransient(response.StatusCode) && attempt < retryCount)
                    {
                        await BackoffAsync(attempt, timeoutCts.Token);
                        continue;
                    }
                    return Result.BusinessError(
                        $"GHN {operation} failed – HTTP {(int)response.StatusCode}: {Truncate(responseText)}");
                }

                var body = JsonSerializer.Deserialize<GhnApiResponse<JsonElement>>(responseText, JsonOptions);
                if (body is null)
                {
                    if (attempt < retryCount) { await BackoffAsync(attempt, timeoutCts.Token); continue; }
                    return Result.Failure("GHN_EMPTY_RESPONSE", $"GHN {operation} returned empty body.");
                }

                if (body.Code != 200)
                {
                    return Result.BusinessError(
                        $"GHN {operation} business error (code={body.Code}): {body.Message}");
                }

                return Result.Success();
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "GHN {Operation} timeout – attempt {Attempt}/{RetryCount}",
                    operation, attempt, retryCount);
                if (attempt < retryCount) { await BackoffAsync(attempt, cancellationToken); continue; }
                return Result.Failure("GHN_TIMEOUT", $"GHN {operation} timed out after {retryCount} attempt(s).");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "GHN {Operation} HTTP error – attempt {Attempt}/{RetryCount}",
                    operation, attempt, retryCount);
                if (attempt < retryCount) { await BackoffAsync(attempt, cancellationToken); continue; }
                return Result.Failure("GHN_HTTP_ERROR", $"GHN {operation} network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GHN {Operation} unexpected error", operation);
                return Result.Failure("GHN_ERROR", $"Unexpected GHN {operation} error: {ex.Message}");
            }
        }

        return Result.Failure("GHN_ERROR", $"GHN {operation} failed after {retryCount} retries.");
    }

    private static bool IsTransient(HttpStatusCode code)
           => code == HttpStatusCode.RequestTimeout
           || code == HttpStatusCode.TooManyRequests
           || (int)code >= 500;

    private static Task BackoffAsync(int attempt, CancellationToken ct)
          => Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);

    private static bool IsSenderConfigured(ShopAddressOptions opts)
         => !string.IsNullOrWhiteSpace(opts.Name)
         && !string.IsNullOrWhiteSpace(opts.Phone)
         && !string.IsNullOrWhiteSpace(opts.AddressLine)
         && !string.IsNullOrWhiteSpace(opts.WardName)
         && !string.IsNullOrWhiteSpace(opts.DistrictName);


    private static int RoundToInt(decimal value)
        => Convert.ToInt32(Math.Round(value, MidpointRounding.AwayFromZero));

    private static string Truncate(string input)
        => string.IsNullOrWhiteSpace(input) ? string.Empty
         : input.Length <= ResponseTruncateLength ? input
         : input[..ResponseTruncateLength];

    private static Result<TTarget> MapFailure<TTarget, TSource>(Result<TSource> source)
        => source.ValidationErrors is not null
            ? Result<TTarget>.ValidationFailure(source.ValidationErrors)
            : Result<TTarget>.Failure(source.ErrorCode ?? "ERROR", source.ErrorMessage ?? "Request failed.");

    private static Result<T> MapFailure<T>(Result source)
        => source.ValidationErrors is not null
            ? Result<T>.ValidationFailure(source.ValidationErrors)
            : Result<T>.Failure(source.ErrorCode ?? "ERROR", source.ErrorMessage ?? "Request failed.");

    private sealed class GhnApiResponse<TData>
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")] public TData? Data { get; set; }
    }

    private sealed class GhnFeeData
    {
        [JsonPropertyName("total")] public decimal Total { get; set; }
    }

    private sealed class GhnAvailableServiceData
    {
        [JsonPropertyName("service_id")] public int ServiceId { get; set; }
        [JsonPropertyName("service_type_id")] public int ServiceTypeId { get; set; }
        [JsonPropertyName("short_name")] public string? ShortName { get; set; }
    }

    private sealed class GhnLeadtimeData
    {
        [JsonPropertyName("leadtime")] public long Leadtime { get; set; }
    }

    private sealed class GhnCreateOrderData
    {
        [JsonPropertyName("order_code")] public string OrderCode { get; set; } = string.Empty;
        [JsonPropertyName("sort_code")] public string? SortCode { get; set; }
        [JsonPropertyName("expected_delivery_time")] public DateTime? ExpectedDeliveryTime { get; set; }
        [JsonPropertyName("total_fee")] public decimal TotalFee { get; set; }
    }

}