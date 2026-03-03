namespace EcommerceAPI.Infrastructure.Settings;

public class RefundRetrySettings
{
    public bool Enabled { get; set; } = true;
    public int MaxAttempts { get; set; } = 3;
    public int InitialDelayMinutes { get; set; } = 5;
    public int BackoffMultiplier { get; set; } = 2;
}
