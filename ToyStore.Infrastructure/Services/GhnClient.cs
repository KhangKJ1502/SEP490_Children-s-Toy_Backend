using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        var serviceId = request.ServiceId is > 0 ? request.ServiceId : null;
        int? preferredTypeId = request.ServiceTypeId is > 0
            ? request.ServiceTypeId
            : (_ghnOptions.FeeServiceTypeId > 0 ? _ghnOptions.FeeServiceTypeId : null);

        int resolvedServiceId;
        if (serviceId.HasValue)
        {
            resolvedServiceId = serviceId.Value;
        }
        else
        {
            var resolveResult = await ResolveServiceIdAsync(request.ToDistrictId, preferredTypeId, cancellationToken);
            if (!resolveResult.IsSuccess)
                return MapFailure<FeeResponseDTO, int>(resolveResult);

            resolvedServiceId = resolveResult.Data;
        }

        var payload = new Dictionary<string, object>
        {
            ["from_district_id"] = request.FromDistrictId,
            ["from_ward_code"] = request.FromWardCode,
            ["to_district_id"] = request.ToDistrictId,
            ["to_ward_code"] = request.ToWardCode,
            ["insurance_value"] = RoundToInt(request.InsuranceValue),
            ["cod_value"] = RoundToInt(request.CodValue),
            ["weight"] = request.Weight,
            ["length"] = request.Length,
            ["width"] = request.Width,
            ["height"] = request.Height,
            ["service_id"] = resolvedServiceId
        };

        var feeResult = await PostAsync<GhnFeeData>("v2/shipping-order/fee", payload, "fee", cancellationToken);
        if (!feeResult.IsSuccess)
            return MapFailure<FeeResponseDTO, GhnFeeData>(feeResult);

        return Result<FeeResponseDTO>.Success(new FeeResponseDTO
        {
            Fee = feeResult.Data!.Total,
            ServiceId = resolvedServiceId
        });
    }

    public async Task<Result<LeadtimeResponseDTO>> GetLeadtimeAsync(
        LeadtimeRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            from_district_id = request.FromDistrictId,
            from_ward_code = request.FromWardCode,
            to_district_id = request.ToDistrictId,
            to_ward_code = request.ToWardCode,
            service_id = request.ServiceId
        };

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

        int resolvedServiceId = request.ServiceId;
        int? resolvedServiceTypeId = null;

        if (resolvedServiceId <= 0)
        {
            // 1. Resolve tu preferred type hoac mac dinh cua route
            int? preferredTypeId = _ghnOptions.FeeServiceTypeId > 0 ? _ghnOptions.FeeServiceTypeId : null;
            var resolveResult = await ResolveServiceIdAsync(request.ToDistrictId, preferredTypeId, cancellationToken);
            
            if (resolveResult.IsSuccess)
            {
                resolvedServiceId = resolveResult.Data;
                resolvedServiceTypeId = preferredTypeId;
            }
            else if (_ghnOptions.DefaultServiceId > 0)
            {
                // 2. Fallback ve default hardcoded neu resolve loi
                _logger.LogWarning("GHN dynamic service resolution failed: {Error}. Falling back to DefaultServiceId={DefaultId}", 
                    resolveResult.ErrorMessage, _ghnOptions.DefaultServiceId);
                resolvedServiceId = _ghnOptions.DefaultServiceId;
                resolvedServiceTypeId = _ghnOptions.FeeServiceTypeId > 0 ? _ghnOptions.FeeServiceTypeId : null;
            }
            else
            {
                // 3. That bai hoan toan
                return MapFailure<ShippingOrderCreateResponseDto, int>(resolveResult);
            }
        }

        var payload = new
        {
            payment_type_id = 2,
            note = request.Note ?? string.Empty,
            required_note = request.RequiredNote,
            from_name = _shopAddressOptions.Name,
            from_phone = _shopAddressOptions.Phone,
            from_address = _shopAddressOptions.AddressLine,
            from_ward_name = _shopAddressOptions.WardName,
            from_district_name = _shopAddressOptions.DistrictName,
            to_name = request.ToName,
            to_phone = request.ToPhone,
            to_address = request.ToAddress,
            to_ward_code = request.ToWardCode,
            to_district_id = request.ToDistrictId,
            cod_amount = RoundToInt(request.CodAmount),
            content = $"Order {request.ClientOrderCode}",
            weight = request.Weight,
            length = request.Length,
            width = request.Width,
            height = request.Height,
            insurance_value = Math.Min(RoundToInt(request.InsuranceValue), 5000000),
            service_id = resolvedServiceId,
            service_type_id = resolvedServiceTypeId,
            client_order_code = request.ClientOrderCode,
            items = request.Items.Select(x => new
            {
                name = x.Name,
                quantity = x.Quantity,
                price = RoundToInt(x.Price),
                weight = x.Weight
            }).ToList()
        };

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
            ServiceId = resolvedServiceId,
            ExpectedDeliveryTime = createResult.Data.ExpectedDeliveryTime
        });
    }

    private async Task<Result<int>> ResolveServiceIdAsync(
        int toDistrictId,
        int? preferredServiceTypeId,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            shop_id = _ghnOptions.ShopId,
            from_district = _ghnOptions.FromDistrictId,
            to_district = toDistrictId
        };

        var servicesResult = await PostAsync<List<GhnAvailableServiceData>>(
            "v2/shipping-order/available-services",
            payload,
            "available_services",
            cancellationToken);

        if (!servicesResult.IsSuccess)
            return MapFailure<int, List<GhnAvailableServiceData>>(servicesResult);

        var services = servicesResult.Data ?? [];
        _logger.LogInformation("GHN available services for district {ToDistrictId}: {Services}", 
            toDistrictId, string.Join(", ", services.Select(s => $"{s.ShortName}(id={s.ServiceId}, type={s.ServiceTypeId})")));

        var selected = services
            .Where(s => s.ServiceId > 0)
            .Where(s => !preferredServiceTypeId.HasValue || s.ServiceTypeId == preferredServiceTypeId.Value)
            .OrderBy(s => s.ServiceId)
            .FirstOrDefault();

        if (selected is null)
        {
            var msg = preferredServiceTypeId.HasValue
                ? $"No GHN service found for service_type_id={preferredServiceTypeId.Value} on this route."
                : "No GHN service found for this route.";
            return Result<int>.Failure("GHN_SERVICE_UNAVAILABLE", msg);
        }

        _logger.LogInformation(
            "Resolved GHN service_id={ServiceId} (type={ServiceTypeId}) for district={ToDistrictId}",
            selected.ServiceId, selected.ServiceTypeId, toDistrictId);

        return Result<int>.Success(selected.ServiceId);
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
    }
}