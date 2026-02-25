using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class StartSupportConversationRequest : IDto
{
    public string Subject { get; set; } = "Canlı Destek";
    public string? InitialMessage { get; set; }
}
