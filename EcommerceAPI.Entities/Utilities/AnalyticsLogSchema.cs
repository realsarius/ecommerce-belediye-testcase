namespace EcommerceAPI.Entities.Utilities;

public static class AnalyticsLogSchema
{
    public static class Streams
    {
        public const string Fulfillment = "fulfillment";
        public const string Returns = "returns";
        public const string Refunds = "refunds";
    }

    public static class Events
    {
        public const string OrderShipped = "order_shipped";
        public const string ReturnRequestReviewed = "return_request_reviewed";
        public const string RefundProcessed = "refund_processed";
        public const string RefundFailed = "refund_failed";
    }
}
