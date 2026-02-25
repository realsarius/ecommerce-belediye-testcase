using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class AssignSupportConversationRequest : IDto
{
    public int SupportUserId { get; set; }
}
