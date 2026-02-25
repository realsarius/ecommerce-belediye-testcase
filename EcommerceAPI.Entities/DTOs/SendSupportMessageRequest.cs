using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Entities.DTOs;

public class SendSupportMessageRequest : IDto
{
    public string Message { get; set; } = string.Empty;
}
