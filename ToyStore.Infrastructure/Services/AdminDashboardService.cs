using Microsoft.EntityFrameworkCore;
using ToyStore.Application.DTOs.Dashboard;
using ToyStore.Application.Interfaces.Services;
using ToyStore.Domain.Constants;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private const byte CustomerRoleId = 1;

    private readonly SEP490ToyStoreContext _context;
    private readonly ITimeProvider _timeProvider;

    public AdminDashboardService(
        SEP490ToyStoreContext context,
        ITimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DashboardOrderStatusStatisticsDto>> GetOrderStatusStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRangePair(filter);
        if (resolved.IsFailure)
        {
            return ToRangeFailure<DashboardOrderStatusStatisticsDto>(resolved);
        }

        var ranges = resolved.Data!;

        var baseQuery = _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Where(o => !(o.PaymentMethod == "SE_PAY" && o.PaymentStatus != "PAID" && o.PaymentStatus != "REFUNDED" && o.PaymentStatus != "PARTIALLY_REFUNDED"))
            .Where(o => o.OrderDate >= ranges.Current.StartUtc && o.OrderDate < ranges.Current.EndUtcExclusive);

        var totalOrders = await baseQuery.CountAsync(cancellationToken);

        var groupedStatusRows = await baseQuery
            .GroupBy(o => o.Status.StatusName)
            .Select(g => new { Status = g.Key, Value = g.Count() })
            .ToListAsync(cancellationToken);

        var allStatuses = await _context.StatusOrders
            .AsNoTracking()
            .OrderBy(x => x.StatusId)
            .Select(x => x.StatusName)
            .ToListAsync(cancellationToken);

        var statusValueMap = groupedStatusRows.ToDictionary(x => x.Status, x => x.Value);
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

        var timelineRows = await baseQuery
            .Select(o => new DashboardOrderStatusEventRowDto
            {
                OrderDateUtc = o.OrderDate,
                Status = o.Status.StatusName
            })
            .ToListAsync(cancellationToken);

        var buckets = BuildBuckets(ranges.Current);
        var timelineCountMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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

    public async Task<Result<DashboardNewCustomerStatisticsDto>> GetNewCustomerStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRangePair(filter);
        if (resolved.IsFailure)
        {
            return ToRangeFailure<DashboardNewCustomerStatisticsDto>(resolved);
        }

        var ranges = resolved.Data!;

        var currentRows = await _context.Accounts
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.RoleId == CustomerRoleId)
            .Where(a => a.CreatedAt >= ranges.Current.StartUtc && a.CreatedAt < ranges.Current.EndUtcExclusive)
            .Select(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var previousTotal = await _context.Accounts
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.RoleId == CustomerRoleId)
            .Where(a => a.CreatedAt >= ranges.Previous.StartUtc && a.CreatedAt < ranges.Previous.EndUtcExclusive)
            .CountAsync(cancellationToken);

        var buckets = BuildBuckets(ranges.Current);
        var valueByBucket = CountByBucket(currentRows, ranges.Current, buckets);

        var details = buckets
            .Select(b => new DashboardCountChartPointDto
            {
                Label = b.Label,
                Date = b.Start,
                Value = valueByBucket[b.Index]
            })
            .ToList();

        var currentTotal = currentRows.Count;

        return Result<DashboardNewCustomerStatisticsDto>.Success(new DashboardNewCustomerStatisticsDto
        {
            Range = ToRangeDto(ranges.Current),
            TotalNewCustomers = currentTotal,
            PreviousPeriodNewCustomers = previousTotal,
            GrowthPercentage = CalculateGrowthPercentage(currentTotal, previousTotal),
            Details = details
        });
    }

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

    private static string NormalizePeriod(string? period)
    {
        return (period ?? DashboardTimePeriods.CurrentMonth).Trim().ToLowerInvariant();
    }

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

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }

    private static DateTime StartOfQuarter(DateTime date)
    {
        var quarterIndex = (date.Month - 1) / 3;
        var startMonth = quarterIndex * 3 + 1;
        return new DateTime(date.Year, startMonth, 1);
    }

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

    private static decimal CalculateGrowthPercentage(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0m : 100m;
        }

        return Math.Round(((current - previous) / previous) * 100m, 2);
    }

    private static decimal CalculateGrowthPercentage(int current, int previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0m : 100m;
        }

        return Math.Round(((decimal)(current - previous) / previous) * 100m, 2);
    }

    private static decimal CalculatePercentage(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0m;
        }

        return Math.Round((decimal)numerator * 100m / denominator, 2);
    }

    private static Result<T> ToRangeFailure<T>(Result<DashboardTimeRangePairDto> resolved)
    {
        return Result<T>.Failure(
            resolved.ErrorCode!,
            resolved.ErrorMessage!);
    }

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
