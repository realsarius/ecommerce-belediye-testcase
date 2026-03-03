using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Business.Extensions;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Entities.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using InvoiceType = EcommerceAPI.Entities.Enums.InvoiceType;

namespace EcommerceAPI.Business.Concrete;

public class OrderManager : IOrderService
{
    private readonly IOrderDal _orderDal;
    private readonly IProductDal _productDal;
    private readonly IInventoryService _inventoryService;
    private readonly ICartService _cartService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICouponService _couponService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IGiftCardService _giftCardService;
    private readonly IReferralService _referralService;
    private readonly IAuditService _auditService;
    private readonly ILogger<OrderManager> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    private const decimal FreeShippingThreshold = 1000m;
    private const decimal ShippingCost = 29.90m;
    private const int OrderTimeoutMinutes = 30;

    public OrderManager(
        IOrderDal orderDal,
        IProductDal productDal,
        IInventoryService inventoryService,
        ICartService cartService,
        IUnitOfWork unitOfWork,
        ICouponService couponService,
        ILoyaltyService loyaltyService,
        IGiftCardService giftCardService,
        IReferralService referralService,
        IAuditService auditService,
        ILogger<OrderManager> logger,
        IPublishEndpoint publishEndpoint)
    {
        _orderDal = orderDal;
        _productDal = productDal;
        _inventoryService = inventoryService;
        _cartService = cartService;
        _unitOfWork = unitOfWork;
        _couponService = couponService;
        _loyaltyService = loyaltyService;
        _giftCardService = giftCardService;
        _referralService = referralService;
        _auditService = auditService;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    [LogAspect]
    [ValidationAspect(typeof(CheckoutRequestValidator))]
    public async Task<IDataResult<OrderDto>> CheckoutAsync(int userId, CheckoutRequest request)
    {
        if (!request.PreliminaryInfoAccepted || !request.DistanceSalesContractAccepted)
        {
            return new ErrorDataResult<OrderDto>("Yasal onaylar tamamlanmadan sipariş oluşturulamaz.");
        }

        var invoiceValidationError = ValidateInvoiceInfo(request.InvoiceInfo);
        if (invoiceValidationError != null)
        {
            return new ErrorDataResult<OrderDto>(invoiceValidationError);
        }

        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
        request.IdempotencyKey = idempotencyKey;

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingResult = await TryGetExistingIdempotentOrderAsync(idempotencyKey);
            if (existingResult != null)
            {
                return existingResult;
            }
        }

        var cartResult = await ValidateCartAndStockAsync(userId);
        if (!cartResult.Success) return new ErrorDataResult<OrderDto>(cartResult.Message);
        var cartDto = cartResult.Data;
        var subtotal = cartDto.TotalAmount;
        var shippingCost = CalculateShippingCost(subtotal);
        var couponResult = await ValidateCouponAsync(request.CouponCode, subtotal);
        if (!couponResult.Success) return new ErrorDataResult<OrderDto>(couponResult.Message);
        var couponData = couponResult.Data;
        var amountAfterCoupon = Math.Max(0m, subtotal - couponData.Amount + shippingCost);
        var loyaltyRedemption = await ResolveLoyaltyRedemptionAsync(userId, request.LoyaltyPointsToUse, amountAfterCoupon);
        if (!loyaltyRedemption.Success) return new ErrorDataResult<OrderDto>(loyaltyRedemption.Message);
        var amountAfterLoyalty = Math.Max(0m, amountAfterCoupon - loyaltyRedemption.Data.DiscountAmount);
        var giftCardRedemption = await ResolveGiftCardRedemptionAsync(userId, request.GiftCardCode, amountAfterLoyalty);
        if (!giftCardRedemption.Success) return new ErrorDataResult<OrderDto>(giftCardRedemption.Message);


        var productQuantities = cartDto.Items.ToDictionary(i => i.ProductId, i => i.Quantity);
        var order = CreateOrderEntity(userId, request, cartDto, subtotal, shippingCost, couponData, loyaltyRedemption.Data, giftCardRedemption.Data);

        OrderCreatedEvent? orderCreatedEvent = null;

        await _unitOfWork.BeginTransactionAsync();
        try
        {

            var stockReservation = await _inventoryService.ReserveStocksAsync(productQuantities, userId, "Order Reservation: " + order.OrderNumber);
            if (!stockReservation.Success)
            {

                await _unitOfWork.RollbackTransactionAsync();
                return new ErrorDataResult<OrderDto>($"Stok rezervasyon hatası: {stockReservation.Message}");
            }


            await _orderDal.AddAsync(order);
            var requiresEarlyPersistence = order.LoyaltyPointsUsed > 0 || order.GiftCardAmount > 0;

            if (requiresEarlyPersistence)
            {
                await _unitOfWork.SaveChangesAsync();

                var loyaltyRedeemResult = await _loyaltyService.RedeemPointsForOrderAsync(
                    userId,
                    order.Id,
                    order.LoyaltyPointsUsed,
                    order.LoyaltyDiscountAmount,
                    $"Sipariş için puan kullanımı ({order.OrderNumber})");

                if (!loyaltyRedeemResult.Success)
                {
                    throw new InvalidOperationException(loyaltyRedeemResult.Message);
                }
            }

            if (order.GiftCardAmount > 0 && !string.IsNullOrWhiteSpace(order.GiftCardCode))
            {
                var giftCardRedeemResult = await _giftCardService.RedeemForOrderAsync(
                    userId,
                    order.Id,
                    order.GiftCardCode,
                    order.GiftCardAmount,
                    $"Sipariş için gift card kullanımı ({order.OrderNumber})");

                if (!giftCardRedeemResult.Success)
                {
                    throw new InvalidOperationException(giftCardRedeemResult.Message);
                }
            }

            await ProcessCouponUsageAsync(order, couponData.Id);
            
            await _cartService.ClearCartAsync(userId);

            await _unitOfWork.SaveChangesAsync();

            orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = userId,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                IdempotencyKey = request.IdempotencyKey
            };

            await _publishEndpoint.Publish(orderCreatedEvent);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "OrderCreatedEvent queued to MassTransit bus outbox. OrderId={OrderId}, OrderNumber={OrderNumber}",
                order.Id,
                order.OrderNumber);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (DbUpdateException ex) when (!string.IsNullOrEmpty(idempotencyKey))
        {
            await _unitOfWork.RollbackTransactionAsync();

            var existingResult = await TryGetExistingIdempotentOrderAsync(idempotencyKey);
            if (existingResult != null)
            {
                _logger.LogInformation(
                    "Checkout replay detected after persistence conflict. UserId={UserId}, IdempotencyKey={IdempotencyKey}",
                    userId,
                    idempotencyKey);

                return existingResult;
            }

            return new ErrorDataResult<OrderDto>($"Sipariş oluşturulamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();

            return new ErrorDataResult<OrderDto>($"Sipariş oluşturulamadı: {ex.Message}");
        }

        var createdOrder = await _orderDal.GetByIdWithDetailsAsync(order.Id);
        if (createdOrder?.Status == OrderStatus.Paid && createdOrder.Payment?.Status == PaymentStatus.Success)
        {
            var referralAwardResult = await _referralService.AwardFirstPurchaseRewardsAsync(createdOrder.Id);
            if (!referralAwardResult.Success)
            {
                return new ErrorDataResult<OrderDto>(referralAwardResult.Message);
            }

            await _unitOfWork.SaveChangesAsync();
            createdOrder = await _orderDal.GetByIdWithDetailsAsync(order.Id);
        }

        await _auditService.LogActionAsync(
            userId.ToString(),
            "CreateOrder",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber, TotalAmount = order.TotalAmount });
        
        return new SuccessDataResult<OrderDto>(createdOrder!.ToDto(), Messages.OrderCreated);
    }

    private async Task<IDataResult<OrderDto>?> TryGetExistingIdempotentOrderAsync(string idempotencyKey)
    {
        var existingOrder = await _orderDal.GetAsync(o => o.Payment != null && o.Payment.IdempotencyKey == idempotencyKey);
        if (existingOrder == null)
        {
            return null;
        }

        var existingOrderDetails = await _orderDal.GetByIdWithDetailsAsync(existingOrder.Id);
        if (existingOrderDetails == null)
        {
            return null;
        }

        return new SuccessDataResult<OrderDto>(existingOrderDetails.ToDto(), "Order already created (Idempotent)");
    }

    private static string? NormalizeIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        return idempotencyKey.Trim();
    }

    [LogAspect]
    public async Task<IDataResult<OrderDto>> GetOrderAsync(int userId, int orderId)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null || order.UserId != userId)
            return new ErrorDataResult<OrderDto>(Messages.OrderNotFound);

        return new SuccessDataResult<OrderDto>(order.ToDto());
    }

    [LogAspect]
    public async Task<IDataResult<List<OrderDto>>> GetUserOrdersAsync(int userId)
    {
        var orders = await _orderDal.GetUserOrdersAsync(userId);
        return new SuccessDataResult<List<OrderDto>>(orders.Select(x => x.ToDto()).ToList());
    }

    [LogAspect]
    public async Task<IDataResult<OrderDto>> CancelOrderAsync(int userId, int orderId, string? status = null)
    {
        if (!string.IsNullOrEmpty(status) && status != "Cancelled")
            return new ErrorDataResult<OrderDto>("Only 'Cancelled' status is supported via this endpoint logic currently.");

        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null || order.UserId != userId)
            return new ErrorDataResult<OrderDto>(Messages.OrderNotFound);

        if (order.Status != OrderStatus.PendingPayment)
            return new ErrorDataResult<OrderDto>(Messages.OnlyPendingOrderCanBeCancelled);

        var previousStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;


        var stockReturns = order.OrderItems.ToDictionary(i => i.ProductId, i => i.Quantity);
        

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _inventoryService.ReleaseStocksAsync(stockReturns, userId, $"Sipariş İptali - Sipariş No: {order.OrderNumber}");
            if (order.LoyaltyPointsUsed > 0)
            {
                var restoreResult = await _loyaltyService.RestoreRedeemedPointsAsync(
                    userId,
                    order.Id,
                    $"Sipariş iptali sonrası puan iadesi ({order.OrderNumber})");

                if (!restoreResult.Success)
                {
                    throw new InvalidOperationException(restoreResult.Message);
                }
            }
            if (order.GiftCardAmount > 0)
            {
                var giftCardRestoreResult = await _giftCardService.RestoreForOrderAsync(
                    userId,
                    order.Id,
                    $"Sipariş iptali sonrası gift card iadesi ({order.OrderNumber})");

                if (!giftCardRestoreResult.Success)
                {
                    throw new InvalidOperationException(giftCardRestoreResult.Message);
                }
            }
            _orderDal.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch(Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw; 
        }

        await _auditService.LogActionAsync(
            userId.ToString(),
            "CancelOrder",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber, PreviousStatus = previousStatus.ToString() });

        return new SuccessDataResult<OrderDto>(order.ToDto(), Messages.OrderCancelled);
    }

    [LogAspect]
    public async Task<IResult> CancelExpiredOrdersAsync()
    {
        var expiryTime = DateTime.UtcNow.AddMinutes(-OrderTimeoutMinutes);
        var expiredOrders = await _orderDal.GetExpiredPendingOrdersAsync(expiryTime);

        if (!expiredOrders.Any()) return new SuccessResult();


        foreach (var order in expiredOrders)
        {
            try
            {
                order.Status = OrderStatus.Cancelled;
                order.Notes = (order.Notes ?? "") + " | [Sistem] Ödeme zaman aşımı nedeniyle iptal edildi.";
                order.CancelledAt = DateTime.UtcNow;

                var stockReturns = order.OrderItems.ToDictionary(i => i.ProductId, i => i.Quantity);
                
                await _inventoryService.ReleaseStocksAsync(stockReturns, order.UserId, $"Sistem İptali - Sipariş No: {order.OrderNumber}");
                if (order.LoyaltyPointsUsed > 0)
                {
                    var restoreResult = await _loyaltyService.RestoreRedeemedPointsAsync(
                        order.UserId,
                        order.Id,
                        $"Ödeme zaman aşımı sonrası puan iadesi ({order.OrderNumber})");

                    if (!restoreResult.Success)
                    {
                        throw new InvalidOperationException(restoreResult.Message);
                    }
                }
                if (order.GiftCardAmount > 0)
                {
                    var giftCardRestoreResult = await _giftCardService.RestoreForOrderAsync(
                        order.UserId,
                        order.Id,
                        $"Ödeme zaman aşımı sonrası gift card iadesi ({order.OrderNumber})");

                    if (!giftCardRestoreResult.Success)
                    {
                        throw new InvalidOperationException(giftCardRestoreResult.Message);
                    }
                }
                _orderDal.Update(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş iptal edilirken hata: OrderId={OrderId}", order.Id);
            }
        }
        
        await _unitOfWork.SaveChangesAsync();
        return new SuccessResult($"{expiredOrders.Count} adet zaman aşımına uğrayan sipariş iptal edildi.");
    }

    [LogAspect]
    public async Task<IDataResult<List<OrderDto>>> GetAllOrdersAsync(string? status = null, decimal? minAmount = null, DateTime? from = null, DateTime? to = null)
    {
        var orders = await _orderDal.GetAllOrdersWithDetailsAsync();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            {
                return new ErrorDataResult<List<OrderDto>>($"{Messages.InvalidOrderStatus}: {status}");
            }

            orders = orders.Where(order => order.Status == parsedStatus).ToList();
        }

        if (minAmount.HasValue)
        {
            orders = orders.Where(order => order.TotalAmount >= minAmount.Value).ToList();
        }

        if (from.HasValue)
        {
            var fromBoundary = from.Value.TimeOfDay == TimeSpan.Zero
                ? from.Value.Date
                : from.Value;
            orders = orders.Where(order => order.CreatedAt >= fromBoundary).ToList();
        }

        if (to.HasValue)
        {
            var toBoundary = to.Value.TimeOfDay == TimeSpan.Zero
                ? to.Value.Date.AddDays(1).AddTicks(-1)
                : to.Value;
            orders = orders.Where(order => order.CreatedAt <= toBoundary).ToList();
        }

        return new SuccessDataResult<List<OrderDto>>(orders.Select(x => x.ToDto()).ToList());
    }

    [LogAspect]
    public async Task<IDataResult<OrderDto>> GetAdminOrderAsync(int orderId)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null)
        {
            return new ErrorDataResult<OrderDto>(Messages.OrderNotFound);
        }

        return new SuccessDataResult<OrderDto>(order.ToDto());
    }

    [LogAspect]
    public async Task<IDataResult<List<OrderDto>>> GetOrdersForSellerAsync(int sellerId)
    {
        var orders = await _orderDal.GetOrdersBySellerIdAsync(sellerId);
        return new SuccessDataResult<List<OrderDto>>(orders.Select(x => x.ToDto()).ToList());
    }

    [LogAspect]
    public async Task<IDataResult<OrderDto>> GetSellerOrderAsync(int sellerId, int orderId)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null)
        {
            return new ErrorDataResult<OrderDto>(Messages.OrderNotFound);
        }

        var isSellerOrder = order.OrderItems.Any(oi => oi.Product.SellerId == sellerId);
        if (!isSellerOrder)
        {
            return new ErrorDataResult<OrderDto>(Messages.OrderNotBelongToUser);
        }

        return new SuccessDataResult<OrderDto>(order.ToDto());
    }

    [LogAspect]
    public async Task<IDataResult<OrderDto>> UpdateOrderStatusAsync(int orderId, string status, int? sellerId = null)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null) return new ErrorDataResult<OrderDto>(Messages.OrderNotFound);

        if (sellerId.HasValue)
        {
            var isSellerOrder = order.OrderItems.Any(oi => oi.Product.SellerId == sellerId.Value);
            if (!isSellerOrder)
                return new ErrorDataResult<OrderDto>(Messages.OrderNotBelongToUser);
        }

        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            return new ErrorDataResult<OrderDto>($"{Messages.InvalidOrderStatus}: {status}");
        }

        var previousStatus = order.Status;
        if (previousStatus == orderStatus)
        {
            return new SuccessDataResult<OrderDto>(order.ToDto(), Messages.OrderStatusUpdated);
        }

        order.Status = orderStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            _orderDal.Update(order);

            await _publishEndpoint.Publish(new OrderStatusChangedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                CustomerEmail = order.User?.Email ?? string.Empty,
                CustomerName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : string.Empty,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = orderStatus.ToString(),
                ChangedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Sipariş durumu güncellenemedi. OrderId={OrderId}", orderId);
            return new ErrorDataResult<OrderDto>($"Sipariş durumu güncellenemedi: {ex.Message}");
        }

        await _auditService.LogActionAsync(
            sellerId?.ToString() ?? "Admin",
            "UpdateOrderStatus",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber, PreviousStatus = previousStatus.ToString(), NewStatus = orderStatus.ToString() });

        return new SuccessDataResult<OrderDto>(order.ToDto(), Messages.OrderStatusUpdated);
    }

    [LogAspect]
    public async Task<IDataResult<OrderDto>> ShipOrderAsync(int sellerId, int orderId, ShipOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrackingCode) || string.IsNullOrWhiteSpace(request.CargoCompany))
        {
            return new ErrorDataResult<OrderDto>("Kargo firması ve takip kodu zorunludur.");
        }

        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order == null)
        {
            return new ErrorDataResult<OrderDto>(Messages.OrderNotFound);
        }

        var isSellerOrder = order.OrderItems.Any(oi => oi.Product.SellerId == sellerId);
        if (!isSellerOrder)
        {
            return new ErrorDataResult<OrderDto>(Messages.OrderNotBelongToUser);
        }

        if (order.Status is not OrderStatus.Paid and not OrderStatus.Processing)
        {
            return new ErrorDataResult<OrderDto>("Sadece ödenmiş veya hazırlanmakta olan siparişler kargoya verilebilir.");
        }

        var previousStatus = order.Status;
        var shippedAt = DateTime.UtcNow;

        order.Status = OrderStatus.Shipped;
        order.CargoCompany = request.CargoCompany.Trim();
        order.TrackingCode = request.TrackingCode.Trim();
        order.ShippedAt = shippedAt;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            _orderDal.Update(order);

            await _publishEndpoint.Publish(new OrderShippedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                CustomerEmail = order.User?.Email ?? string.Empty,
                CustomerName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : string.Empty,
                CargoCompany = order.CargoCompany,
                TrackingCode = order.TrackingCode,
                ShippedAt = shippedAt
            });

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Sipariş kargoya verilemedi. OrderId={OrderId}, SellerId={SellerId}", orderId, sellerId);
            return new ErrorDataResult<OrderDto>($"Sipariş kargoya verilemedi: {ex.Message}");
        }

        await _auditService.LogActionAsync(
            sellerId.ToString(),
            "ShipOrder",
            "Order",
            new
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                PreviousStatus = previousStatus.ToString(),
                NewStatus = order.Status.ToString(),
                order.CargoCompany,
                order.TrackingCode
            });

        return new SuccessDataResult<OrderDto>(order.ToDto(), "Sipariş kargoya verildi.");
    }

    [LogAspect]
    public async Task<IDataResult<OrderDto>> UpdateOrderItemsAsync(int userId, int orderId, UpdateOrderItemsRequest request)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        
        var validationResult = ValidateOrderForUpdate(order, userId);
        if (!validationResult.Success) return new ErrorDataResult<OrderDto>(validationResult.Message);

        if (request.Items == null || !request.Items.Any())
            return new ErrorDataResult<OrderDto>(Messages.OrderMustHaveItems);

        var products = await LoadProductsForUpdateAsync(request.Items);
        if (products.Count != request.Items.Select(i => i.ProductId).Distinct().Count())
            return new ErrorDataResult<OrderDto>(Messages.SomeProductsNotFound);

        var existingItems = order!.OrderItems.ToDictionary(oi => oi.ProductId, oi => oi);
        var newItems = request.Items.ToDictionary(i => i.ProductId, i => i);

        var (itemsToRemove, itemsToAdd, itemsToUpdate) = IdentifyChanges(existingItems, newItems);


        var stockValidation = ValidateStockAvailability(products, newItems, itemsToAdd, itemsToUpdate, existingItems);
        if (!stockValidation.Success) return new ErrorDataResult<OrderDto>(stockValidation.Message);

        var stockDeltas = new Dictionary<int, int>();

        foreach (var pid in itemsToRemove)
        {
            var qty = existingItems[pid].Quantity;
            if (stockDeltas.ContainsKey(pid)) stockDeltas[pid] += qty; else stockDeltas[pid] = qty;
        }

        foreach (var pid in itemsToAdd)
        {
            var qty = newItems[pid].Quantity;
            if (stockDeltas.ContainsKey(pid)) stockDeltas[pid] -= qty; else stockDeltas[pid] = -qty;
        }

        foreach (var pid in itemsToUpdate)
        {
            var oldQty = existingItems[pid].Quantity;
            var newQty = newItems[pid].Quantity;
            var diff = newQty - oldQty; 
            
            if (diff != 0)
            {
               if (stockDeltas.ContainsKey(pid)) stockDeltas[pid] -= diff; else stockDeltas[pid] = -diff; 
            }
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (stockDeltas.Any(x => x.Value != 0))
            {
                var stockResult = await _inventoryService.BulkAdjustStocksAsync(stockDeltas, userId, $"Sipariş Düzenleme - Sipariş No: {order.OrderNumber}");
                if (!stockResult.Success) throw new Exception(stockResult.Message);
            }

            ApplyOrderChanges(order, itemsToRemove, itemsToAdd, itemsToUpdate, newItems, products);
            RecalculateOrderTotals(order);
            
            _orderDal.Update(order);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return new ErrorDataResult<OrderDto>($"Sipariş güncellenemedi: {ex.Message}");
        }

        var updatedOrder = await _orderDal.GetByIdWithDetailsAsync(order.Id);
        await _auditService.LogActionAsync(
            userId.ToString(),
            "UpdateOrderItems",
            "Order",
            new { OrderId = order.Id, OrderNumber = order.OrderNumber, NewTotalAmount = order.TotalAmount });
        
        return new SuccessDataResult<OrderDto>(updatedOrder!.ToDto(), Messages.OrderItemsUpdated);
    }

    private IResult ValidateOrderForUpdate(Order? order, int userId)
    {
        if (order == null || order.UserId != userId)
            return new ErrorResult(Messages.OrderNotFound);
        if (order.Status != OrderStatus.PendingPayment)
            return new ErrorResult(Messages.OnlyPendingOrderCanBeUpdated);
        if (order.LoyaltyPointsUsed > 0)
            return new ErrorResult("Sadakat puanı kullanılan siparişler düzenlenemez.");
        if (order.GiftCardAmount > 0)
            return new ErrorResult("Gift card kullanılan siparişler düzenlenemez.");
        return new SuccessResult();
    }

    private async Task<Dictionary<int, Product>> LoadProductsForUpdateAsync(List<UpdateOrderItemDto> items)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productDal.GetByIdsWithInventoryAsync(productIds);
        return products.ToDictionary(p => p.Id, p => p);
    }

    private (List<int> Removed, List<int> Added, List<int> Updated) IdentifyChanges(
        Dictionary<int, OrderItem> existingItems,
        Dictionary<int, UpdateOrderItemDto> newItems)
    {
        var removed = existingItems.Keys.Except(newItems.Keys).ToList();
        var added = newItems.Keys.Except(existingItems.Keys).ToList();
        var updated = existingItems.Keys.Intersect(newItems.Keys).ToList();
        return (removed, added, updated);
    }

    private IResult ValidateStockAvailability(
        Dictionary<int, Product> products,
        Dictionary<int, UpdateOrderItemDto> newItems,
        List<int> addedIds,
        List<int> updatedIds,
        Dictionary<int, OrderItem> existingItems)
    {
        foreach (var productId in addedIds)
        {
            var requestedQty = newItems[productId].Quantity;
            var availableStock = products[productId].Inventory?.QuantityAvailable ?? 0;
            if (requestedQty > availableStock)
                return new ErrorResult($"{Messages.StockInsufficient}: {products[productId].Name}");
        }

        foreach (var productId in updatedIds)
        {
            var existingQty = existingItems[productId].Quantity;
            var requestedQty = newItems[productId].Quantity;
            var qtyDiff = requestedQty - existingQty;
            
            if (qtyDiff > 0)
            {
                var availableStock = products[productId].Inventory?.QuantityAvailable ?? 0;
                if (qtyDiff > availableStock)
                    return new ErrorResult($"{Messages.StockInsufficient}: {products[productId].Name}");
            }
        }
        return new SuccessResult();
    }

    private void ApplyOrderChanges(
        Order order,
        List<int> itemsToRemove,
        List<int> itemsToAdd,
        List<int> itemsToUpdate,
        Dictionary<int, UpdateOrderItemDto> newItems,
        Dictionary<int, Product> products)
    {
        var existingItems = order.OrderItems.ToDictionary(i => i.ProductId);

        foreach (var pid in itemsToRemove)
        {
            order.OrderItems.Remove(existingItems[pid]);
        }

        foreach (var pid in itemsToAdd)
        {
            var product = products[pid];
            order.OrderItems.Add(new OrderItem
            {
                ProductId = pid,
                Quantity = newItems[pid].Quantity,
                PriceSnapshot = product.GetEffectivePrice()
            });
        }

        foreach (var pid in itemsToUpdate)
        {
            existingItems[pid].Quantity = newItems[pid].Quantity;
        }
    }

    private void RecalculateOrderTotals(Order order)
    {
        decimal subtotal = order.OrderItems.Sum(oi => oi.PriceSnapshot * oi.Quantity);
        decimal shippingCost = subtotal >= FreeShippingThreshold ? 0 : ShippingCost;
        order.TotalAmount = subtotal - order.DiscountAmount + shippingCost - order.LoyaltyDiscountAmount - order.GiftCardAmount;
        if (order.Payment != null)
        {
            order.Payment.Amount = order.TotalAmount;
        }
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }

    private async Task<IDataResult<CartDto>> ValidateCartAndStockAsync(int userId)
    {
        var cartResult = await _cartService.GetCartAsync(userId);
        if (!cartResult.Success) return new ErrorDataResult<CartDto>(cartResult.Message);

        var cartDto = cartResult.Data;
        if (cartDto == null || !cartDto.Items.Any())
            return new ErrorDataResult<CartDto>(Messages.CartEmpty);

        foreach (var item in cartDto.Items)
        {
            if (item.Quantity > item.AvailableStock)
                return new ErrorDataResult<CartDto>($"{Messages.StockInsufficient}: {item.ProductName}");
        }

        return new SuccessDataResult<CartDto>(cartDto);
    }

    private decimal CalculateShippingCost(decimal subtotal)
    {
        return subtotal >= FreeShippingThreshold ? 0 : ShippingCost;
    }

    private async Task<IDataResult<(int? Id, string? Code, decimal Amount)>> ValidateCouponAsync(string? couponCode, decimal subtotal)
    {
        if (string.IsNullOrEmpty(couponCode))
            return new SuccessDataResult<(int? Id, string? Code, decimal Amount)>((null, null, 0));

        var couponResult = await _couponService.ValidateCouponAsync(couponCode, subtotal);
        if (!couponResult.Success)
            return new ErrorDataResult<(int? Id, string? Code, decimal Amount)>("Kupon doğrulanamadı.");
        
        var validation = couponResult.Data!;
        if (!validation.IsValid)
            return new ErrorDataResult<(int? Id, string? Code, decimal Amount)>(validation.ErrorMessage ?? Messages.CouponInvalid);
        
        return new SuccessDataResult<(int? Id, string? Code, decimal Amount)>((validation.Coupon!.Id, validation.Coupon.Code, validation.DiscountAmount));
    }

    private Order CreateOrderEntity(
        int userId,
        CheckoutRequest request,
        CartDto cartDto,
        decimal subtotal,
        decimal shippingCost,
        (int? Id, string? Code, decimal Amount) couponData,
        LoyaltyRedemptionPreviewDto loyaltyRedemption,
        GiftCardValidationResult giftCardRedemption)
    {
        var acceptedAt = DateTime.UtcNow;
        var order = new Order
        {
            UserId = userId,
            OrderNumber = GenerateOrderNumber(),
            Status = giftCardRedemption.FinalTotal <= 0 ? OrderStatus.Paid : OrderStatus.PendingPayment,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes ?? string.Empty,
            PreliminaryInfoAcceptedAt = request.PreliminaryInfoAccepted ? acceptedAt : null,
            DistanceSalesContractAcceptedAt = request.DistanceSalesContractAccepted ? acceptedAt : null,
            AcceptedFromIp = string.IsNullOrWhiteSpace(request.AcceptedFromIp) ? null : request.AcceptedFromIp,
            SubtotalAmount = subtotal,
            TotalAmount = subtotal - couponData.Amount + shippingCost - loyaltyRedemption.DiscountAmount - giftCardRedemption.AppliedAmount,
            CouponId = couponData.Id,
            CouponCode = couponData.Code,
            DiscountAmount = couponData.Amount,
            LoyaltyPointsUsed = loyaltyRedemption.AppliedPoints,
            LoyaltyDiscountAmount = loyaltyRedemption.DiscountAmount,
            GiftCardId = giftCardRedemption.GiftCardId > 0 ? giftCardRedemption.GiftCardId : null,
            GiftCardCode = string.IsNullOrWhiteSpace(giftCardRedemption.Code) ? null : giftCardRedemption.Code,
            GiftCardAmount = giftCardRedemption.AppliedAmount,
            InvoiceInfo = CreateInvoiceInfo(request.InvoiceInfo!)
        };

        foreach (var cartItem in cartDto.Items)
        {
            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                PriceSnapshot = cartItem.UnitPrice 
            });
        }

        order.Payment = new Payment
        {
            Amount = order.TotalAmount,
            Status = order.TotalAmount <= 0 ? PaymentStatus.Success : PaymentStatus.Pending,
            PaymentMethod = order.TotalAmount <= 0 ? "GiftCard" : request.PaymentMethod,
            Provider = order.TotalAmount <= 0 ? null : PaymentProviderType.Iyzico,
            IdempotencyKey = !string.IsNullOrEmpty(request.IdempotencyKey) ? request.IdempotencyKey : Guid.NewGuid().ToString()
        };

        return order;
    }

    private async Task ProcessCouponUsageAsync(Order order, int? couponId)
    {
        if (couponId.HasValue)
        {
            var usageResult = await _couponService.IncrementUsageAsync(couponId.Value);
            if (!usageResult.Success)
                throw new Exception("Kupon kullanımı kaydedilemedi.");
        }
    }

    private async Task<IDataResult<LoyaltyRedemptionPreviewDto>> ResolveLoyaltyRedemptionAsync(int userId, int? requestedPoints, decimal amountAfterCoupon)
    {
        if (!requestedPoints.HasValue || requestedPoints.Value <= 0)
        {
            return new SuccessDataResult<LoyaltyRedemptionPreviewDto>(new LoyaltyRedemptionPreviewDto());
        }

        return await _loyaltyService.CalculateRedemptionAsync(userId, requestedPoints.Value, amountAfterCoupon);
    }

    private async Task<IDataResult<GiftCardValidationResult>> ResolveGiftCardRedemptionAsync(int userId, string? giftCardCode, decimal amountAfterDiscounts)
    {
        if (string.IsNullOrWhiteSpace(giftCardCode))
        {
            return new SuccessDataResult<GiftCardValidationResult>(new GiftCardValidationResult
            {
                IsValid = true,
                FinalTotal = amountAfterDiscounts
            });
        }

        return await _giftCardService.ValidateAsync(userId, giftCardCode, amountAfterDiscounts);
    }

    private static string? ValidateInvoiceInfo(CheckoutInvoiceInfoRequest? invoiceInfo)
    {
        if (invoiceInfo == null)
        {
            return "Fatura bilgisi zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(invoiceInfo.InvoiceAddress) || invoiceInfo.InvoiceAddress.Length < 10)
        {
            return "Fatura adresi zorunludur.";
        }

        if (invoiceInfo.Type == InvoiceType.Corporate)
        {
            if (string.IsNullOrWhiteSpace(invoiceInfo.CompanyName))
            {
                return "Kurumsal fatura için şirket adı zorunludur.";
            }

            if (string.IsNullOrWhiteSpace(invoiceInfo.TaxOffice))
            {
                return "Kurumsal fatura için vergi dairesi zorunludur.";
            }

            if (string.IsNullOrWhiteSpace(invoiceInfo.TaxNumber) || invoiceInfo.TaxNumber.Length != 10 || invoiceInfo.TaxNumber.Any(ch => !char.IsDigit(ch)))
            {
                return "Kurumsal fatura için 10 haneli vergi numarası zorunludur.";
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(invoiceInfo.FullName))
        {
            return "Bireysel fatura için ad soyad zorunludur.";
        }

        if (!string.IsNullOrWhiteSpace(invoiceInfo.TcKimlikNo)
            && (invoiceInfo.TcKimlikNo.Length != 11 || invoiceInfo.TcKimlikNo.Any(ch => !char.IsDigit(ch))))
        {
            return "TC kimlik numarası 11 haneli olmalıdır.";
        }

        return null;
    }

    private static InvoiceInfo CreateInvoiceInfo(CheckoutInvoiceInfoRequest invoiceInfo)
    {
        return new InvoiceInfo
        {
            Type = invoiceInfo.Type,
            FullName = invoiceInfo.Type == InvoiceType.Corporate
                ? (invoiceInfo.FullName ?? invoiceInfo.CompanyName ?? string.Empty)
                : invoiceInfo.FullName ?? string.Empty,
            TcKimlikNo = string.IsNullOrWhiteSpace(invoiceInfo.TcKimlikNo) ? null : invoiceInfo.TcKimlikNo,
            CompanyName = string.IsNullOrWhiteSpace(invoiceInfo.CompanyName) ? null : invoiceInfo.CompanyName,
            TaxOffice = string.IsNullOrWhiteSpace(invoiceInfo.TaxOffice) ? null : invoiceInfo.TaxOffice,
            TaxNumber = string.IsNullOrWhiteSpace(invoiceInfo.TaxNumber) ? null : invoiceInfo.TaxNumber,
            InvoiceAddress = invoiceInfo.InvoiceAddress
        };
    }
}
