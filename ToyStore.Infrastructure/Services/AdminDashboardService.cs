using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Dashboard;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services;

/// <summary>
/// Dịch vụ quản lý và thống kê báo cáo cho trang Dashboard của Admin.
/// </summary>
public class AdminDashboardService : IAdminDashboardService
{
    private const byte CustomerRoleId = 1;

    private readonly SEP490ToyStoreContext _context;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Khởi tạo dịch vụ AdminDashboardService với DbContext và TimeProvider.
    /// </summary>
    public AdminDashboardService(
        SEP490ToyStoreContext context,
        ITimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Thống kê tỷ lệ và số lượng đơn hàng theo từng trạng thái (ví dụ: Đã giao, Chờ xử lý, Đã hủy...) trong khoảng thời gian bộ lọc.
    /// </summary>
    public async Task<Result<DashboardOrderStatusStatisticsDto>> GetOrderStatusStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        // 1. Phân tích khoảng thời gian truy vấn (Kỳ này & Kỳ trước) từ bộ lọc của client gửi lên
        var resolved = ResolveRangePair(filter);
        if (resolved.IsFailure)
        {
            return ToRangeFailure<DashboardOrderStatusStatisticsDto>(resolved);
        }

        var ranges = resolved.Data!;

        // 2. Xây dựng truy vấn cơ sở lấy các đơn hàng hợp lệ trong kỳ hiện tại:
        // - Đơn hàng chưa bị xóa (IsDeleted = false)
        // - Không tính các đơn hàng sử dụng cổng SE_PAY nhưng chưa thanh toán thành công (để tránh nhiễu dữ liệu)
        // - Ngày đặt hàng nằm trong khoảng thời gian của kỳ hiện tại
        var baseQuery = _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Where(o => !(o.PaymentMethod == "SE_PAY" && o.PaymentStatus != "PAID" && o.PaymentStatus != "REFUNDED" && o.PaymentStatus != "PARTIALLY_REFUNDED"))
            .Where(o => o.OrderDate >= ranges.Current.StartUtc && o.OrderDate < ranges.Current.EndUtcExclusive);

        // 3. Đếm tổng số lượng đơn hàng hợp lệ
        var totalOrders = await baseQuery.CountAsync(cancellationToken);

        // 4. Gom nhóm đơn hàng theo trạng thái và đếm số lượng của từng trạng thái trong DB
        var groupedStatusRows = await baseQuery
            .GroupBy(o => o.Status.StatusName)
            .Select(g => new { Status = g.Key, Value = g.Count() })
            .ToListAsync(cancellationToken);

        // 5. Lấy danh sách tất cả các trạng thái đơn hàng hiện có để đảm bảo thống kê đầy đủ (kể cả trạng thái có 0 đơn hàng)
        var allStatuses = await _context.StatusOrders
            .AsNoTracking()
            .OrderBy(x => x.StatusId)
            .Select(x => x.StatusName)
            .ToListAsync(cancellationToken);

        var statusValueMap = groupedStatusRows.ToDictionary(x => x.Status, x => x.Value);
        
        // 6. Ánh xạ danh sách trạng thái kèm số lượng và tính phần trăm tỷ lệ tương ứng trên tổng đơn hàng
        var statuses = allStatuses
            .Select(statusName =>
            {
                statusValueMap.TryGetValue(statusName, out var statusValue);
                return new DashboardOrderStatusItemDto
                {
                    Status = statusName,
                    Value = statusValue,
                    Percentage = CalculatePercentage(statusValue, totalOrders)
                };
            })
            .ToList();

        // 7. Lấy danh sách thô ngày đặt hàng và trạng thái của các đơn hàng để phân bổ vào biểu đồ timeline
        var timelineRows = await baseQuery
            .Select(o => new DashboardOrderStatusEventRowDto
            {
                OrderDateUtc = o.OrderDate,
                Status = o.Status.StatusName
            })
            .ToListAsync(cancellationToken);

        // 8. Phân chia kỳ hiện tại thành các phân đoạn (buckets) nhỏ (theo Ngày, Tuần, Tháng) để làm mốc trục X biểu đồ
        var buckets = BuildBuckets(ranges.Current);
        var timelineCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        // 9. Duyệt qua từng đơn hàng để đếm phân bổ số lượng vào từng phân đoạn thời gian và trạng thái tương ứng
        foreach (var row in timelineRows)
        {
            var localOrderDate = _timeProvider.ToVnTime(row.OrderDateUtc);
            var bucketIndex = ResolveBucketIndex(localOrderDate, ranges.Current);
            if (bucketIndex < 0)
            {
                continue;
            }

            var key = $"{bucketIndex}|{row.Status}";
            timelineCountMap.TryGetValue(key, out var currentCount);
            timelineCountMap[key] = currentCount + 1;
        }

        // 10. Khởi dựng chi tiết biểu đồ đảm bảo hiển thị đầy đủ mọi trạng thái ở từng mốc thời gian (nếu không có đơn hàng thì số lượng mặc định là 0)
        var details = new List<DashboardOrderStatusTimelinePointDto>();
        foreach (var bucket in buckets)
        {
            foreach (var statusName in allStatuses)
            {
                var key = $"{bucket.Index}|{statusName}";
                timelineCountMap.TryGetValue(key, out var value);
                details.Add(new DashboardOrderStatusTimelinePointDto
                {
                    Label = bucket.Label,
                    Date = bucket.Start,
                    Status = statusName,
                    Value = value
                });
            }
        }

        return Result<DashboardOrderStatusStatisticsDto>.Success(new DashboardOrderStatusStatisticsDto
        {
            Range = ToRangeDto(ranges.Current),
            TotalOrders = totalOrders,
            Statuses = statuses,
            Details = details
        });
    }

    /// <summary>
    /// Thống kê số lượng khách hàng đăng ký mới và tính phần trăm tăng trưởng so với kỳ trước.
    /// </summary>
    public async Task<Result<DashboardNewCustomerStatisticsDto>> GetNewCustomerStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        // 1. Phân tích khoảng thời gian truy vấn (Kỳ này & Kỳ trước) từ bộ lọc của client
        var resolved = ResolveRangePair(filter);
        if (resolved.IsFailure)
        {
            return ToRangeFailure<DashboardNewCustomerStatisticsDto>(resolved);
        }

        var ranges = resolved.Data!;

        // 2. Truy xuất danh sách thời gian tạo tài khoản của khách hàng mới đăng ký trong kỳ hiện tại
        var currentRows = await _context.Accounts
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.RoleId == CustomerRoleId)
            .Where(a => a.CreatedAt >= ranges.Current.StartUtc && a.CreatedAt < ranges.Current.EndUtcExclusive)
            .Select(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        // 3. Đếm số lượng khách hàng mới đăng ký trong kỳ trước đó để so sánh đối chứng
        var previousTotal = await _context.Accounts
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.RoleId == CustomerRoleId)
            .Where(a => a.CreatedAt >= ranges.Previous.StartUtc && a.CreatedAt < ranges.Previous.EndUtcExclusive)
            .CountAsync(cancellationToken);

        // 4. Chia khoảng thời gian truy vấn thành các phân đoạn (buckets) để hiển thị trục X biểu đồ
        var buckets = BuildBuckets(ranges.Current);
        
        // 5. Đếm số lượng khách hàng đăng ký mới tương ứng rơi vào từng phân đoạn thời gian
        var valueByBucket = CountByBucket(currentRows, ranges.Current, buckets);

        // 6. Ánh xạ dữ liệu biểu đồ chi tiết gửi về client
        var details = buckets
            .Select(b => new DashboardCountChartPointDto
            {
                Label = b.Label,
                Date = b.Start,
                Value = valueByBucket[b.Index]
            })
            .ToList();

        var currentTotal = currentRows.Count;

        // 7. Trả về thống kê tổng hợp kèm theo tỷ lệ tăng trưởng so với kỳ trước
        return Result<DashboardNewCustomerStatisticsDto>.Success(new DashboardNewCustomerStatisticsDto
        {
            Range = ToRangeDto(ranges.Current),
            TotalNewCustomers = currentTotal,
            PreviousPeriodNewCustomers = previousTotal,
            GrowthPercentage = CalculateGrowthPercentage(currentTotal, previousTotal),
            Details = details
        });
    }

    /// <summary>
    /// Thống kê chỉ số doanh thu (trừ đi tiền hoàn trả đơn hàng), số lượng đơn hàng và tỷ lệ tăng trưởng so với kỳ trước.
    /// </summary>
    public async Task<Result<DashboardGrowthStatisticsDto>> GetGrowthStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRangePair(filter);
        if (resolved.IsFailure)
        {
            return ToRangeFailure<DashboardGrowthStatisticsDto>(resolved);
        }

        var ranges = resolved.Data!;

        var revenueCurrentTotal = await BuildRevenueQuery(ranges.Current)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var revenueCurrentRefunded = await BuildRevenueQuery(ranges.Current)
            .SelectMany(o => o.OrderRefunds)
            .Where(r => !r.IsDeleted && r.StatusId == 8)
            .SumAsync(r => (decimal?)r.ApprovedAmount, cancellationToken) ?? 0m;

        var revenueCurrent = revenueCurrentTotal - revenueCurrentRefunded;

        var revenuePreviousTotal = await BuildRevenueQuery(ranges.Previous)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var revenuePreviousRefunded = await BuildRevenueQuery(ranges.Previous)
            .SelectMany(o => o.OrderRefunds)
            .Where(r => !r.IsDeleted && r.StatusId == 8)
            .SumAsync(r => (decimal?)r.ApprovedAmount, cancellationToken) ?? 0m;

        var revenuePrevious = revenuePreviousTotal - revenuePreviousRefunded;

        var ordersCurrent = await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Where(o => !(o.PaymentMethod == "SE_PAY" && o.PaymentStatus != "PAID" && o.PaymentStatus != "REFUNDED" && o.PaymentStatus != "PARTIALLY_REFUNDED"))
            .Where(o => o.OrderDate >= ranges.Current.StartUtc && o.OrderDate < ranges.Current.EndUtcExclusive)
            .CountAsync(cancellationToken);

        var ordersPrevious = await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Where(o => !(o.PaymentMethod == "SE_PAY" && o.PaymentStatus != "PAID" && o.PaymentStatus != "REFUNDED" && o.PaymentStatus != "PARTIALLY_REFUNDED"))
            .Where(o => o.OrderDate >= ranges.Previous.StartUtc && o.OrderDate < ranges.Previous.EndUtcExclusive)
            .CountAsync(cancellationToken);

        return Result<DashboardGrowthStatisticsDto>.Success(new DashboardGrowthStatisticsDto
        {
            Range = ToRangeDto(ranges.Current),
            RevenueCurrent = revenueCurrent,
            RevenuePrevious = revenuePrevious,
            RevenueGrowthPercentage = CalculateGrowthPercentage(revenueCurrent, revenuePrevious),
            OrdersCurrent = ordersCurrent,
            OrdersPrevious = ordersPrevious,
            OrdersGrowthPercentage = CalculateGrowthPercentage(ordersCurrent, ordersPrevious)
        });
    }

    /// <summary>
    /// Thống kê tổng số lượng sản phẩm đang hoạt động trên hệ thống (chưa bị xóa).
    /// </summary>
    public async Task<Result<DashboardTotalProductsDto>> GetTotalProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalProducts = await _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .CountAsync(cancellationToken);

        return Result<DashboardTotalProductsDto>.Success(new DashboardTotalProductsDto
        {
            TotalProducts = totalProducts
        });
    }

    /// <summary>
    /// Xây dựng truy vấn để tính toán doanh thu thực tế (chỉ tính các đơn hàng có trạng thái Đã giao, Hoàn thành và đã thanh toán hợp lệ).
    /// </summary>
    private IQueryable<Order> BuildRevenueQuery(DashboardTimeRangeInternalDto range)
    {
        string[] validRevenueStatuses = [OrderStatuses.Delivered, OrderStatuses.Completed];

        return _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Where(o => !(o.PaymentMethod == "SE_PAY" && o.PaymentStatus != "PAID" && o.PaymentStatus != "REFUNDED" && o.PaymentStatus != "PARTIALLY_REFUNDED"))
            .Where(o => validRevenueStatuses.Contains(o.Status.StatusName))
            .Where(o => (o.CompletedAt ?? o.DeliveredAt ?? o.PaidAt ?? o.OrderDate) >= range.StartUtc
                && (o.CompletedAt ?? o.DeliveredAt ?? o.PaidAt ?? o.OrderDate) < range.EndUtcExclusive);
    }

    /// <summary>
    /// Phân tích và chuyển đổi bộ lọc thời gian của Client thành cặp mốc thời gian (Kỳ hiện tại và Kỳ trước) theo giờ Việt Nam và UTC.
    /// </summary>
    private Result<DashboardTimeRangePairDto> ResolveRangePair(DashboardTimeFilterDto filter)
    {
        var normalizedPeriod = NormalizePeriod(filter.Period);
        if (!DashboardTimePeriods.Supported.Contains(normalizedPeriod))
        {
            return Result<DashboardTimeRangePairDto>.Failure(
                "VALIDATION_ERROR",
                $"Unsupported period '{filter.Period}'.");
        }

        var today = _timeProvider.VnNow.Date;

        DateTime currentStartVn;
        DateTime currentEndVnExclusive;
        DateTime previousStartVn;
        DateTime previousEndVnExclusive;

        switch (normalizedPeriod)
        {
            case DashboardTimePeriods.Today:
                currentStartVn = today;
                currentEndVnExclusive = today.AddDays(1);
                previousStartVn = currentStartVn.AddDays(-1);
                previousEndVnExclusive = currentStartVn;
                break;

            case DashboardTimePeriods.CurrentWeek:
                currentStartVn = StartOfWeek(today);
                currentEndVnExclusive = currentStartVn.AddDays(7);
                previousStartVn = currentStartVn.AddDays(-7);
                previousEndVnExclusive = currentStartVn;
                break;

            case DashboardTimePeriods.PreviousWeek:
                currentEndVnExclusive = StartOfWeek(today);
                currentStartVn = currentEndVnExclusive.AddDays(-7);
                previousStartVn = currentStartVn.AddDays(-7);
                previousEndVnExclusive = currentStartVn;
                break;

            case DashboardTimePeriods.CurrentMonth:
                currentStartVn = new DateTime(today.Year, today.Month, 1);
                currentEndVnExclusive = currentStartVn.AddMonths(1);
                previousStartVn = currentStartVn.AddMonths(-1);
                previousEndVnExclusive = currentStartVn;
                break;

            case DashboardTimePeriods.PreviousMonth:
                currentEndVnExclusive = new DateTime(today.Year, today.Month, 1);
                currentStartVn = currentEndVnExclusive.AddMonths(-1);
                previousStartVn = currentStartVn.AddMonths(-1);
                previousEndVnExclusive = currentStartVn;
                break;

            case DashboardTimePeriods.CurrentQuarter:
                currentStartVn = StartOfQuarter(today);
                currentEndVnExclusive = currentStartVn.AddMonths(3);
                previousStartVn = currentStartVn.AddMonths(-3);
                previousEndVnExclusive = currentStartVn;
                break;

            case DashboardTimePeriods.PreviousQuarter:
                currentEndVnExclusive = StartOfQuarter(today);
                currentStartVn = currentEndVnExclusive.AddMonths(-3);
                previousStartVn = currentStartVn.AddMonths(-3);
                previousEndVnExclusive = currentStartVn;
                break;

            default:
                return Result<DashboardTimeRangePairDto>.Failure(
                    "VALIDATION_ERROR",
                    $"Unsupported period '{filter.Period}'.");
        }

        var groupBy = ResolveGroupBy(filter.GroupBy, normalizedPeriod, currentStartVn, currentEndVnExclusive);
        if (!DashboardGroupByModes.Supported.Contains(groupBy))
        {
            return Result<DashboardTimeRangePairDto>.Failure(
                "VALIDATION_ERROR",
                $"Unsupported groupBy '{filter.GroupBy}'.");
        }

        var currentRange = BuildTimeRange(normalizedPeriod, groupBy, currentStartVn, currentEndVnExclusive);
        var previousRange = BuildTimeRange(normalizedPeriod, groupBy, previousStartVn, previousEndVnExclusive);
        return Result<DashboardTimeRangePairDto>.Success(new DashboardTimeRangePairDto(currentRange, previousRange));
    }

    /// <summary>
    /// Khởi tạo cấu trúc mốc thời gian đồng bộ giữa giờ Việt Nam và giờ UTC quốc tế.
    /// </summary>
    private DashboardTimeRangeInternalDto BuildTimeRange(
        string period,
        string groupBy,
        DateTime startVn,
        DateTime endVnExclusive)
    {
        return new DashboardTimeRangeInternalDto
        {
            Period = period,
            GroupBy = groupBy,
            StartVn = startVn,
            EndVnExclusive = endVnExclusive,
            StartUtc = _timeProvider.ToUtc(startVn),
            EndUtcExclusive = _timeProvider.ToUtc(endVnExclusive)
        };
    }

    /// <summary>
    /// Chuẩn hóa định dạng chuỗi của mốc thời gian (mặc định là Tháng hiện tại nếu rỗng).
    /// </summary>
    private static string NormalizePeriod(string? period)
    {
        return (period ?? DashboardTimePeriods.CurrentMonth).Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Xác định chế độ gom nhóm dữ liệu (theo Ngày, Tuần, Tháng) tương ứng với khoảng thời gian truy vấn.
    /// </summary>
    private static string ResolveGroupBy(
        string? requestedGroupBy,
        string period,
        DateTime startVn,
        DateTime endVnExclusive)
    {
        if (!string.IsNullOrWhiteSpace(requestedGroupBy))
        {
            return requestedGroupBy.Trim().ToLowerInvariant();
        }

        if (period is DashboardTimePeriods.CurrentQuarter or DashboardTimePeriods.PreviousQuarter)
        {
            return DashboardGroupByModes.Week;
        }

        var totalDays = (endVnExclusive - startVn).TotalDays;
        if (totalDays <= 31)
        {
            return DashboardGroupByModes.Day;
        }

        if (totalDays <= 120)
        {
            return DashboardGroupByModes.Week;
        }

        return DashboardGroupByModes.Month;
    }

    /// <summary>
    /// Lấy ngày đầu tuần (Thứ hai) của một ngày bất kỳ.
    /// </summary>
    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }

    /// <summary>
    /// Lấy ngày đầu tiên của Quý chứa ngày bất kỳ.
    /// </summary>
    private static DateTime StartOfQuarter(DateTime date)
    {
        var quarterIndex = (date.Month - 1) / 3;
        var startMonth = quarterIndex * 3 + 1;
        return new DateTime(date.Year, startMonth, 1);
    }

    /// <summary>
    /// Chuyển đổi khoảng thời gian nội bộ sang định dạng DTO gửi về Client.
    /// </summary>
    private static DashboardTimeRangeDto ToRangeDto(DashboardTimeRangeInternalDto range)
    {
        return new DashboardTimeRangeDto
        {
            Period = range.Period,
            GroupBy = range.GroupBy,
            FromDate = range.StartVn,
            ToDate = range.EndVnExclusive.AddTicks(-1)
        };
    }

    /// <summary>
    /// Tính toán phần trăm tăng trưởng giữa kỳ hiện tại và kỳ trước (hỗ trợ kiểu decimal).
    /// </summary>
    private static decimal CalculateGrowthPercentage(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0m : 100m;
        }

        return Math.Round(((current - previous) / previous) * 100m, 2);
    }

    /// <summary>
    /// Tính toán phần trăm tăng trưởng giữa kỳ hiện tại và kỳ trước (hỗ trợ kiểu int).
    /// </summary>
    private static decimal CalculateGrowthPercentage(int current, int previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0m : 100m;
        }

        return Math.Round(((decimal)(current - previous) / previous) * 100m, 2);
    }

    /// <summary>
    /// Tính toán tỷ lệ phần trăm (phép chia có làm tròn).
    /// </summary>
    private static decimal CalculatePercentage(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0m;
        }

        return Math.Round((decimal)numerator * 100m / denominator, 2);
    }

    /// <summary>
    /// Helper đóng gói thông báo lỗi khi phân tích mốc thời gian thất bại.
    /// </summary>
    private static Result<T> ToRangeFailure<T>(Result<DashboardTimeRangePairDto> resolved)
    {
        return Result<T>.Failure(
            resolved.ErrorCode!,
            resolved.ErrorMessage!);
    }

    /// <summary>
    /// Gom nhóm và đếm số lượng bản ghi rơi vào từng khoảng (bucket) thời gian của biểu đồ.
    /// </summary>
    private Dictionary<int, int> CountByBucket(
        IEnumerable<DateTime> utcTimes,
        DashboardTimeRangeInternalDto range,
        IReadOnlyCollection<DashboardBucketDto> buckets)
    {
        var valueByBucket = buckets.ToDictionary(b => b.Index, _ => 0);
        foreach (var utcTime in utcTimes)
        {
            var bucketIndex = ResolveBucketIndex(_timeProvider.ToVnTime(utcTime), range);
            if (bucketIndex >= 0)
            {
                valueByBucket[bucketIndex] += 1;
            }
        }

        return valueByBucket;
    }

    /// <summary>
    /// Phân chia khoảng thời gian truy vấn thành các phân đoạn (buckets) nhỏ theo chế độ gom nhóm (Ngày, Tuần, Tháng) để vẽ biểu đồ.
    /// </summary>
    private static List<DashboardBucketDto> BuildBuckets(DashboardTimeRangeInternalDto range)
    {
        var buckets = new List<DashboardBucketDto>();
        var cursor = range.StartVn;
        var index = 0;

        while (cursor < range.EndVnExclusive)
        {
            DateTime next;
            string label;

            if (range.GroupBy == DashboardGroupByModes.Day)
            {
                next = cursor.AddDays(1);
                label = cursor.ToString("dd/MM");
            }
            else if (range.GroupBy == DashboardGroupByModes.Week)
            {
                next = cursor.AddDays(7);
                var labelEnd = next.AddDays(-1);
                label = $"{cursor:dd/MM} - {labelEnd:dd/MM}";
            }
            else
            {
                next = new DateTime(cursor.Year, cursor.Month, 1).AddMonths(1);
                label = cursor.ToString("MM/yyyy");
            }

            if (next > range.EndVnExclusive)
            {
                next = range.EndVnExclusive;
            }

            buckets.Add(new DashboardBucketDto
            {
                Index = index,
                Start = cursor,
                Label = label
            });

            cursor = next;
            index++;
        }

        return buckets;
    }

    /// <summary>
    /// Xác định xem một mốc thời gian cụ thể rơi vào phân đoạn (bucket) thứ mấy trong danh sách.
    /// </summary>
    private static int ResolveBucketIndex(DateTime localTime, DashboardTimeRangeInternalDto range)
    {
        if (localTime < range.StartVn || localTime >= range.EndVnExclusive)
        {
            return -1;
        }

        return range.GroupBy switch
        {
            DashboardGroupByModes.Day => (localTime.Date - range.StartVn.Date).Days,
            DashboardGroupByModes.Week => (int)((localTime.Date - range.StartVn.Date).TotalDays / 7),
            DashboardGroupByModes.Month => ((localTime.Year - range.StartVn.Year) * 12) + (localTime.Month - range.StartVn.Month),
            _ => -1
        };
    }

}
