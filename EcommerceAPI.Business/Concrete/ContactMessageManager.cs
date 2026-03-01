using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class ContactMessageManager : IContactMessageService
{
    private readonly IContactMessageDal _contactMessageDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ContactMessageManager> _logger;

    public ContactMessageManager(
        IContactMessageDal contactMessageDal,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<ContactMessageManager> logger)
    {
        _contactMessageDal = contactMessageDal;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<IDataResult<ContactMessageDto>> CreateAsync(CreateContactMessageRequest request, string? ipAddress, string? userAgent)
    {
        var now = DateTime.UtcNow;

        var message = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _contactMessageDal.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        await _publishEndpoint.Publish(new ContactMessageReceivedEvent
        {
            ContactMessageId = message.Id,
            Name = message.Name,
            Email = message.Email,
            Subject = message.Subject,
            Message = message.Message,
            CreatedAt = now
        });

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Contact message created. ContactMessageId={ContactMessageId}, Email={Email}, Subject={Subject}, IpAddress={IpAddress}",
            message.Id,
            message.Email,
            message.Subject,
            message.IpAddress);

        return new SuccessDataResult<ContactMessageDto>(new ContactMessageDto
        {
            Id = message.Id,
            Name = message.Name,
            Email = message.Email,
            Subject = message.Subject,
            Message = message.Message,
            CreatedAt = message.CreatedAt
        }, "Mesajınız alındı. En kısa sürede size dönüş yapacağız.");
    }
}
