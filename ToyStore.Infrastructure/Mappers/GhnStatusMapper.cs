using System;
using System.Collections.Generic;
using ToyStore.Application.DTOs.Orders;

namespace ToyStore.Infrastructure.Mappers;

public static class GhnStatusMapper
{
    private static readonly Dictionary<string, int> GhnStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Shipped group (StatusID = 4)
        ["ready_to_pick"]           = 4,
        ["picking"]                 = 4,
        ["money_collect_picking"]   = 4,
        ["picked"]                  = 4,

        // Only update ShippingProviderTransactions, SKIP Orders update (return 0)
        ["storing"]                 = 0,
        ["transporting"]            = 0,
        ["sorting"]                 = 0,
        ["return_transporting"]     = 0,
        ["return_sorting"]          = 0,

        // Delivering (StatusID = 5)
        ["delivering"]              = 5,
        ["money_collect_delivering"]= 5,

        // Terminal / Special
        ["delivered"]               = 6,   // Delivered
        ["delivery_fail"]           = 12,  // DeliveryFailed
        ["waiting_to_return"]       = 13,  // WaitingReturn
        ["return"]                  = 10,  // Returning
        ["returning"]               = 10,  // Returning
        ["returned"]                = 11,  // ReturnCompleted
        ["return_fail"]             = 14,  // ReturnFailed
        ["cancel"]                  = 8,   // Cancelled
        ["exception"]               = 12,  // DeliveryFailed (dùng tạm)
        ["damage"]                  = 16,  // Damaged
        ["lost"]                    = 15,  // Lost
    };

    public static int ToInternalStatusId(string ghnStatus)
    {
        if (GhnStatusMap.TryGetValue(ghnStatus, out var statusId))
        {
            return statusId;
        }
        return 0;
    }

    public static string ComputePaymentStatus(GhnWebhookPayload p, string currentPaymentMethod, string currentPaymentStatus)
    {
        return p.Status.ToLowerInvariant() switch
        {
            "delivered" when p.PaymentType == 1              => "PAID",       // COD thu tiền thành công
            "delivered" when p.PaymentType == 2              => currentPaymentStatus, // giữ nguyên (đã thanh toán trước)
            "delivery_fail" when p.PaymentType == 1          => "COD_PENDING",
            "cancel"                                          => "CANCELLED",
            "returned" or "return_fail"
                when currentPaymentStatus == "PAID"          => "REFUNDED",
            _                                                 => currentPaymentStatus
        };
    }
}
