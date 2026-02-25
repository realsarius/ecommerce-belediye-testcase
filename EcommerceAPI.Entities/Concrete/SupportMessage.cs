namespace EcommerceAPI.Entities.Concrete;

public class SupportMessage : BaseEntity
{
    public int ConversationId { get; set; }
    public int SenderUserId { get; set; }
    public string SenderRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSystemMessage { get; set; }

    public SupportConversation Conversation { get; set; } = null!;
    public User SenderUser { get; set; } = null!;
}
