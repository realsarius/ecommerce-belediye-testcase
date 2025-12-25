namespace EcommerceAPI.Core.CrossCuttingConcerns.Logging;

/// <summary>
/// Audit logging servisi için interface.
/// İş katmanında kullanılan işlemlerin izlenebilirliğini sağlar.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Kullanıcı aksiyonlarını audit log olarak kaydeder.
    /// </summary>
    /// <param name="userId">İşlemi yapan kullanıcı ID'si</param>
    /// <param name="action">Gerçekleştirilen aksiyon (örn: CreateOrder, UpdateProduct)</param>
    /// <param name="resource">Etkilenen kaynak (örn: Order, Product)</param>
    /// <param name="data">İşlemle ilgili ek veriler (opsiyonel)</param>
    void LogAction(string userId, string action, string resource, object? data = null);
    
    /// <summary>
    /// Asenkron olarak kullanıcı aksiyonlarını audit log olarak kaydeder.
    /// </summary>
    Task LogActionAsync(string userId, string action, string resource, object? data = null);
}
