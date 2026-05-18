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
    private const string RefundedStatus = "Refunded";

    private readonly SEP490ToyStoreContext _context;
    private readonly ITimeProvider _timeProvider;

    public AdminDashboardService(
        SEP490ToyStoreContext context,
        ITimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DashboardRevenueStatisticsDto>> GetRevenueStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRangePair(filter);
        if (resolved.IsFailure)
        {
            return ToRangeFailure<DashboardRevenueStatisticsDto>(resolved);
        }

        var ranges = resolved.Data!;

        var currentEvents = await BuildRevenueQuery(ranges.Current)
            .Select(o => new DashboardRevenueEventRowDto
            {
                Amount = o.TotalAmount,
                EventAtUtc = o.CompletedAt ?? o.DeliveredAt ?? o.PaidAt ?? o.OrderDate
            })
            .ToListAsync(cancellationToken);

        var previousRevenue = await BuildRevenueQuery(ranges.Previous)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var buckets = BuildBuckets(ranges.Current);
        var amountByBucket = SumRevenueByBucket(currentEvents, ranges.Current, buckets);

        var details = buckets
            .Select(b => new DashboardRevenueChartPointDto
            {
                Label = b.Label,
                Date = b.Start,
                Value = amountByBucket[b.Index]
            })
            .ToList();

        var totalRevenue = currentEvents.Sum(x => x.Amount);

        return Result<DashboardRevenueStatisticsDto>.Success(new DashboardRevenueStatisticsDto
        {
            Range = ToRangeDto(ranges.Current),
            TotalRevenue = totalRevenue,
            PreviousPeriodRevenue = previousRevenue,
            GrowthPercentage = CalculateGrowthPercentage(totalRevenue, previousRevenue),
            Details = details
        });
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

    public async Task<Result<DashboardCompletedOrderStatisticsDto>> GetCompletedOrderStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRangePair(filter);
        if (resolved.IsFailure)
        {
            return ToRangeFailure<DashboardCompletedOrderStatisticsDto>(resolved);
        }

        var ranges = resolved.Data!;

        var currentRows = await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Where(o => o.Status.StatusName == OrderStatuses.Completed)
            .Where(o => o.CompletedAt != null
                && o.CompletedAt >= ranges.Current.StartUtc
                && o.CompletedAt < ranges.Current.EndUtcExclusive)
            .Select(o => o.CompletedAt!.Value)
            .ToListAsync(cancellationToken);

        var previousTotal = await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Where(o => o.Status.StatusName == OrderStatuses.Completed)
            .Where(o => o.CompletedAt != null
                && o.CompletedAt >= ranges.Previous.StartUtc
                && o.CompletedAt < ranges.Previous.EndUtcExclusive)
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

        return Result<DashboardCompletedOrderStatisticsDto>.Success(new DashboardCompletedOrderStatisticsDto
        {
            Range = ToRangeDto(ranges.Current),
            TotalCompletedOrders = currentTotal,
            PreviousPeriodCompletedOrders = previousTotal,
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

        var revenueCurrent = await BuildRevenueQuery(ranges.Current)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var revenuePrevious = await BuildRevenueQuery(ranges.Previous)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var ordersCurrent = await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Where(o => o.OrderDate >= ranges.Current.StartUtc && o.OrderDate < ranges.Current.EndUtcExclusive)
            .CountAsync(cancellationToken);

        var ordersPrevious = await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
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

    public async Task<Result<DashboardOrderRateStatisticsDto>> GetOrderRateStatisticsAsync(
        DashboardTimeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveRangePair(filter);
        if (resolved.IsFailure)
        {
            return ToRangeFailure<DashboardOrderRateStatisticsDto>(resolved);
        }

        var range = resolved.Data!.Current;

        var periodOrders = _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .Where(o => o.OrderDate >= range.StartUtc && o.OrderDate < range.EndUtcExclusive);

        var totalOrders = await periodOrders.CountAsync(cancellationToken);

        var refundedOrders = await periodOrders
            .Where(o => o.Status.StatusName == RefundedStatus)
            .CountAsync(cancellationToken);

        var cancelledOrders = await periodOrders
            .Where(o => o.Status.StatusName == OrderStatuses.Cancelled)
            .CountAsync(cancellationToken);

        return Result<DashboardOrderRateStatisticsDto>.Success(new DashboardOrderRateStatisticsDto
        {
            Range = ToRangeDto(range),
            TotalOrders = totalOrders,
            RefundedOrders = refundedOrders,
            CancelledOrders = cancelledOrders,
            RefundRatePercentage = CalculatePercentage(refundedOrders, totalOrders),
            CancellationRatePercentage = CalculatePercentage(cancelledOrders, totalOrders)
        });
    }

    public async Task<Result<DashboardTopSellingProductsDto>> GetTopSellingProductsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var resolvedLimit = ResolveTopLimit(limit);

        var products = await BuildValidSoldOrderDetailsQuery()
            .GroupBy(od => new
            {
                od.ProductId,
                ProductName = od.Product.ProductName,
                ImageUrl = od.Product.ProductImage != null
                    ? od.Product.ProductImage.ImageUrl
                    : od.ProductImage
            })
            .Select(g => new DashboardTopSellingProductItemDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ImageUrl = g.Key.ImageUrl,
                TotalSold = g.Sum(x => (int)x.Quantity),
                Revenue = g.Sum(x => x.LineTotal ?? 0m)
            })
            .OrderByDescending(x => x.TotalSold)
            .ThenByDescending(x => x.Revenue)
            .ThenBy(x => x.ProductName)
            .Take(resolvedLimit)
            .ToListAsync(cancellationToken);

        return Result<DashboardTopSellingProductsDto>.Success(new DashboardTopSellingProductsDto
        {
            Limit = resolvedLimit,
            TotalItems = products.Count,
            Products = products
        });
    }

    public async Task<Result<DashboardSlowMovingProductsDto>> GetSlowMovingProductsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var resolvedLimit = ResolveSlowMovingLimit(limit);
        var nowUtc = _timeProvider.UtcNow;

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Quantity > 0)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.ProductId)
            .Select(p => new DashboardSlowMovingProductItemDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ImageUrl = p.ProductImage != null ? p.ProductImage.ImageUrl : null,
                QuantityInStock = p.Quantity,
                StockedAt = p.CreatedAt,
                DaysInStock = EF.Functions.DateDiffDay(p.CreatedAt, nowUtc)
            })
            .Take(resolvedLimit)
            .ToListAsync(cancellationToken);

        return Result<DashboardSlowMovingProductsDto>.Success(new DashboardSlowMovingProductsDto
        {
            Limit = resolvedLimit,
            TotalItems = products.Count,
            Products = products
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
            .Where(o => validRevenueStatuses.Contains(o.Status.StatusName))
            .Where(o => (o.CompletedAt ?? o.DeliveredAt ?? o.PaidAt ?? o.OrderDate) >= range.StartUtc
                && (o.CompletedAt ?? o.DeliveredAt ?? o.PaidAt ?? o.OrderDate) < range.EndUtcExclusive);
    }

    private IQueryable<OrderDetail> BuildValidSoldOrderDetailsQuery()
    {
        string[] finalOrderStatuses = [OrderStatuses.Delivered, OrderStatuses.Completed];
        string[] invalidOrderStatuses = [OrderStatuses.Cancelled, OrderStatuses.Refunded];

        return _context.OrderDetails
            .AsNoTracking()
            .Where(od => !od.Order.IsDeleted && !od.Product.IsDeleted)
            .Where(od =>
                (od.Order.PaymentStatus == PaymentStatuses.Paid
                 || finalOrderStatuses.Contains(od.Order.Status.StatusName))
                && !invalidOrderStatuses.Contains(od.Order.Status.StatusName));
    }

    private static int ResolveTopLimit(int requestedLimit)
    {
        if (requestedLimit <= 0)
        {
            return 10;
        }

        return Math.Min(requestedLimit, 50);
    }

    private static int ResolveSlowMovingLimit(int requestedLimit)
    {
        if (requestedLimit <= 0)
        {
            return 5;
        }

        return Math.Min(requestedLimit, 50);
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

    private Dictionary<int, decimal> SumRevenueByBucket(
        IEnumerable<DashboardRevenueEventRowDto> events,
        DashboardTimeRangeInternalDto range,
        IReadOnlyCollection<DashboardBucketDto> buckets)
    {
        var amountByBucket = buckets.ToDictionary(b => b.Index, _ => 0m);
        foreach (var row in events)
        {
            var bucketIndex = ResolveBucketIndex(_timeProvider.ToVnTime(row.EventAtUtc), range);
            if (bucketIndex >= 0)
            {
                amountByBucket[bucketIndex] += row.Amount;
            }
        }

        return amountByBucket;
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
