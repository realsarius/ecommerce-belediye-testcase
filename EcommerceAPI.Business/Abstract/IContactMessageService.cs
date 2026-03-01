using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface IContactMessageService
{
    Task<IDataResult<ContactMessageDto>> CreateAsync(CreateContactMessageRequest request, string? ipAddress, string? userAgent);
}
