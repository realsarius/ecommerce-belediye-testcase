using EcommerceAPI.Entities.Enums;

namespace EcommerceAPI.Entities.Concrete;

public class SupportConversation : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public int CustomerUserId { get; set; }
    public int? SupportUserId { get; set; }
    public SupportConversationStatus Status { get; set; } = SupportConversationStatus.Open;
    public DateTime? LastMessageAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public User CustomerUser { get; set; } = null!;
    public User? SupportUser { get; set; }
    public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
}
