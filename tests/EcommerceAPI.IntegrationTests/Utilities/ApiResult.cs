namespace EcommerceAPI.IntegrationTests.Utilities;

public class ApiResult<T>
{
    public T Data { get; set; } = default!;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
