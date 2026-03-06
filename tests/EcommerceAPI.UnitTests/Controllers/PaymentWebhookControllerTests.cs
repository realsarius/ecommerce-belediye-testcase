using EcommerceAPI.API.Controllers;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Infrastructure.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace EcommerceAPI.UnitTests.Controllers;

public class PaymentWebhookControllerTests
{
    private readonly Mock<IPaymentService> _paymentServiceMock = new();
    private readonly Mock<IOrderDal> _orderDalMock = new();
    private readonly Mock<ILogger<PaymentWebhookController>> _loggerMock = new();

    [Fact]
    public async Task HandleWebhook_WhenSignatureInvalid_ShouldReturnUnauthorized()
    {
        var controller = new PaymentWebhookController(_paymentServiceMock.Object, _orderDalMock.Object, _loggerMock.Object);
        var request = new IyzicoWebhookRequest
        {
            IyziEventType = "PAYMENT",
            PaymentId = "PAY-1",
            PaymentConversationId = "ORD-1",
            Status = "SUCCESS"
        };

        _paymentServiceMock
            .Setup(x => x.ProcessWebhookAsync(request, "invalid-signature"))
            .ReturnsAsync(new ErrorResult("Invalid signature", InfrastructureConstants.Payment.WebhookInvalidSignatureCode));

        var result = await controller.HandleWebhook(request, "invalid-signature");

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorized.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task HandleWebhook_WhenOrderNotFound_ShouldReturnNotFound()
    {
        var controller = new PaymentWebhookController(_paymentServiceMock.Object, _orderDalMock.Object, _loggerMock.Object);
        var request = new IyzicoWebhookRequest
        {
            IyziEventType = "PAYMENT",
            PaymentId = "PAY-2",
            PaymentConversationId = "ORD-404",
            Status = "SUCCESS"
        };

        _paymentServiceMock
            .Setup(x => x.ProcessWebhookAsync(request, "valid-signature"))
            .ReturnsAsync(new ErrorResult("Order not found", InfrastructureConstants.Payment.OrderNotFoundCode));

        var result = await controller.HandleWebhook(request, "valid-signature");

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task HandleWebhook_WhenProcessedSuccessfully_ShouldReturnOk()
    {
        var controller = new PaymentWebhookController(_paymentServiceMock.Object, _orderDalMock.Object, _loggerMock.Object);
        var request = new IyzicoWebhookRequest
        {
            IyziEventType = "PAYMENT",
            PaymentId = "PAY-3",
            PaymentConversationId = "ORD-3",
            Status = "SUCCESS"
        };

        _paymentServiceMock
            .Setup(x => x.ProcessWebhookAsync(request, "valid-signature"))
            .ReturnsAsync(new SuccessResult("Webhook processed"));

        var result = await controller.HandleWebhook(request, "valid-signature");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task HandleWebhook_WhenDuplicateEvent_ShouldReturnOkWithDuplicateMessage()
    {
        var controller = new PaymentWebhookController(_paymentServiceMock.Object, _orderDalMock.Object, _loggerMock.Object);
        var request = new IyzicoWebhookRequest
        {
            IyziEventType = "PAYMENT",
            PaymentId = "PAY-4",
            PaymentConversationId = "ORD-4",
            Status = "SUCCESS"
        };

        _paymentServiceMock
            .Setup(x => x.ProcessWebhookAsync(request, "valid-signature"))
            .ReturnsAsync(new SuccessResult("Webhook already processed"));

        var result = await controller.HandleWebhook(request, "valid-signature");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        var payload = JsonSerializer.Serialize(ok.Value);
        payload.Should().Contain("Webhook already processed");
    }
}
