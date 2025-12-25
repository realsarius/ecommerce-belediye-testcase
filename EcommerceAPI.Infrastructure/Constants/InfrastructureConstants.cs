namespace EcommerceAPI.Infrastructure.Constants;

public static class InfrastructureConstants
{
    public static class Redis
    {
        public const int DefaultLockTimeoutSeconds = 10;
        public const int CartCacheDays = 7;
        public const string SystemBusyCode = "SYSTEM_BUSY";
        public const string SystemBusyMessage = "Sistem yoğunluğu nedeniyle işlem gerçekleştirilemedi. Lütfen tekrar deneyin.";
    }

    public static class Payment
    {
        public const string DefaultErrorCode = "PAYMENT_FAILED";
        public const string DefaultErrorMessage = "Ödeme işlemi başarısız oldu.";
        public const string OrderNotFoundCode = "ORDER_NOT_FOUND";
        public const string PaymentRecordNotFoundCode = "PAYMENT_RECORD_NOT_FOUND";
        public const string AlreadyPaidCode = "ALREADY_PAID";
    }
}
