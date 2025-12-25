namespace EcommerceAPI.Infrastructure.Settings;

/// <summary>
/// Iyzico ödeme entegrasyonu ayarları.
/// appsettings.json veya environment variables üzerinden yapılandırılır.
/// </summary>
public class IyzicoSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";
}
