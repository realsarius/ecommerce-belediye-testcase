using EcommerceAPI.Core.DataAccess.EntityFramework;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.DataAccess.Concrete.EntityFramework.Contexts;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.DataAccess.Concrete.EntityFramework;

public class EfPaymentWebhookEventDal : EfEntityRepositoryBase<PaymentWebhookEvent, AppDbContext>, IPaymentWebhookEventDal
{
    public EfPaymentWebhookEventDal(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> TryAddWebhookEventAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var createdAt = webhookEvent.CreatedAt == default ? DateTime.UtcNow : webhookEvent.CreatedAt;
        var updatedAt = webhookEvent.UpdatedAt == default ? createdAt : webhookEvent.UpdatedAt;

        var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""TBL_PaymentWebhookEvents""
            (""Provider"", ""DedupeKey"", ""EventType"", ""ProviderEventId"", ""PaymentId"", ""PaymentConversationId"", ""Status"", ""EventTime"", ""CreatedAt"", ""UpdatedAt"")
            VALUES ({(int)webhookEvent.Provider}, {webhookEvent.DedupeKey}, {webhookEvent.EventType}, {webhookEvent.ProviderEventId}, {webhookEvent.PaymentId}, {webhookEvent.PaymentConversationId}, {webhookEvent.Status}, {webhookEvent.EventTime}, {createdAt}, {updatedAt})
            ON CONFLICT (""Provider"", ""DedupeKey"") DO NOTHING;
            ", cancellationToken);

        return rowsAffected > 0;
    }
}
