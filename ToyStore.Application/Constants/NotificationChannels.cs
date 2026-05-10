namespace ToyStore.Application.Constants;

public static class NotificationChannels
{
    public const string WebBell = "WEB_BELL";
    public const string Email   = "EMAIL";
    public const string WebPush = "WEB_PUSH";
}

public static class NotificationTypes
{
    public const string Order     = "ORDER";
    public const string Promotion = "PROMOTION";
    public const string System    = "SYSTEM";
    public const string Blog      = "BLOG";
    public const string Stock     = "STOCK";
}

public static class RecipientTypes
{
    public const string Customer    = "CUSTOMER";
    public const string Staff       = "STAFF";
    public const string Admin       = "ADMIN";
    public const string Merchandise = "MERCHANDISE";
}

public static class EmailStatuses
{
    public const string Pending = "Pending";
    public const string Sent    = "Sent";
    public const string Failed  = "Failed";
}

public static class NotificationStatuses
{
    public const string Unread   = "Unread";
    public const string Read     = "Read";
    public const string Archived = "Archived";
}

public static class PreferenceKeys
{
    public const string OrderUpdates = "OrderUpdates";
    public const string Promotions   = "Promotions";
    public const string StockAlerts  = "StockAlerts";
    public const string BlogAlerts   = "BlogAlerts";
}
