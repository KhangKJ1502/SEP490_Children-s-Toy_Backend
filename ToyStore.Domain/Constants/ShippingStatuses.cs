namespace ToyStore.Domain.Constants;

public static class ShippingStatuses
{
    // GHN / GHTK Statuses
    public const string ReadyToPick           = "ready_to_pick";
    public const string Picking               = "picking";
    public const string Picked                = "picked";
    public const string Storing               = "storing";
    public const string Transporting         = "transporting";
    public const string Sorting               = "sorting";
    public const string Delivering            = "delivering";
    public const string MoneyCollectDelivering = "money_collect_delivering";
    public const string Delivered             = "delivered";
    public const string Cancel                = "cancel";
    public const string DeliveryFail          = "delivery_fail";
    public const string WaitingToReturn       = "waiting_to_return";
    public const string Return                = "return";
    public const string ReturnTransporting    = "return_transporting";
    public const string ReturnSorting         = "return_sorting";
    public const string Returning             = "returning";
    public const string ReturnFail            = "return_fail";
    public const string Returned              = "returned";
    public const string Exception             = "exception";
    public const string Damage                = "damage";
    public const string Lost                  = "lost";
}
