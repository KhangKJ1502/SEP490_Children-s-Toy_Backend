namespace ToyStore.Domain.Constants;

public static class OrderCancelReasons
{
    public const string DeliveryFailedGhn = "DELIVERY_FAILED_GHN";
    public const string GhnCancelled = "GHN_CANCELLED";
    public const string DamagedInTransit = "DAMAGED_IN_TRANSIT";
    public const string LostInTransit = "LOST_IN_TRANSIT";
}
