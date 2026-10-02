using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManagement.Model.DTOs.Requests;
using OrderManagement.Model.DTOs.Responses;
using OrderManagement.Model.Entities;
using OrderManagement.Model.Enums;
using OrderManagement.Model.Interfaces;
using OrderManagement.Model.StateMachine;
using OrderManagement.Service.Exceptions;

namespace OrderManagement.Service;

public sealed class OrderService(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Creating order for customer {CustomerId} with idempotency key {IdempotencyKey}",
            request.CustomerId, idempotencyKey);
        
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds, ct);

        if (products.Count != productIds.Count)
        {
            var missing = productIds.Except(products.Select(p => p.Id)).First();
            throw new NotFoundException(nameof(Product), missing);
        }

        var productMap = products.ToDictionary(p => p.Id);        
        
        await orderRepository.BeginTransactionAsync(ct);
        
        try
        {
            foreach (var item in request.Items)
            {
                var product = productMap[item.ProductId];
                var success = await productRepository.TryDeductStockAsync(item.ProductId, item.Quantity, ct);
                if (!success)
                {
                    throw new InsufficientStockException(product.Name, product.StockQuantity, item.Quantity);
                }
            }
        }
        catch
        {
            await orderRepository.RollbackTransactionAsync(ct);
            throw;
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Items = request.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = productMap[i.ProductId].Price   
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        try
        {
            await orderRepository.AddAsync(order, ct);
            await orderRepository.SaveChangesAsync(ct);
            await orderRepository.CommitTransactionAsync(ct);

            logger.LogInformation("Order {OrderId} created successfully", order.Id);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogWarning("Duplicate idempotency key {IdempotencyKey} — returning existing order", idempotencyKey);
            await orderRepository.RollbackTransactionAsync(ct);

            var existing = await orderRepository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
            return MapToResponse(existing!);
        }
        catch
        {
            await orderRepository.RollbackTransactionAsync(ct);
            throw;
        }
        
        var created = await orderRepository.GetByIdWithItemsAsync(order.Id, ct);
        return MapToResponse(created!);
    }

    public async Task<OrderResponse> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdWithItemsAsync(orderId, ct)
            ?? throw new NotFoundException(nameof(Order), orderId);

        return MapToResponse(order);
    }

    public async Task<PagedResponse<OrderResponse>> ListOrdersAsync(
        ListOrdersRequest request,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await orderRepository.ListAsync(request, ct);

        return new PagedResponse<OrderResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdWithItemsAsync(orderId, ct)
            ?? throw new NotFoundException(nameof(Order), orderId);

        if (!OrderStatusTransition.IsValid(order.Status, request.Status))
        {
            var allowed = OrderStatusTransition.GetAllowed(order.Status)
                          .Select(s => s.ToString());
            throw new InvalidStatusTransitionException(
                order.Status.ToString(), request.Status.ToString(), allowed);
        }

        logger.LogInformation(
            "Transitioning order {OrderId} from {From} to {To}",
            orderId, order.Status, request.Status);

        order.Status = request.Status;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await orderRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                $"Order {orderId} was modified by another request. Please reload and retry.");
        }

        return MapToResponse(order);
    }

    public async Task<OrderResponse> CancelOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdWithItemsAsync(orderId, ct)
            ?? throw new NotFoundException(nameof(Order), orderId);

        if (!OrderStatusTransition.IsValid(order.Status, OrderStatus.Cancelled))
        {
            var allowed = OrderStatusTransition.GetAllowed(order.Status)
                          .Select(s => s.ToString());
            throw new InvalidStatusTransitionException(
                order.Status.ToString(), OrderStatus.Cancelled.ToString(), allowed);
        }

        logger.LogInformation("Cancelling order {OrderId}", orderId);

        await orderRepository.BeginTransactionAsync(ct);

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {            
            await orderRepository.SaveChangesAsync(ct);
            
            foreach (var item in order.Items)
                await productRepository.RestoreStockAsync(item.ProductId, item.Quantity, ct);

            await orderRepository.CommitTransactionAsync(ct);
            logger.LogInformation("Order {OrderId} cancelled, stock restored", orderId);
        }
        catch (DbUpdateConcurrencyException)
        {
            await orderRepository.RollbackTransactionAsync(ct);
            throw new ConflictException(
                $"Order {orderId} was modified by another request. Please reload and retry.");
        }
        catch
        {
            await orderRepository.RollbackTransactionAsync(ct);
            throw;
        }

        return MapToResponse(order);
    }

    private static OrderResponse MapToResponse(Order order) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        Status = order.Status,
        ShippingAddress = order.ShippingAddress,
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.Items.Select(i => new OrderItemResponse
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
        
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("IX_Orders_IdempotencyKey", StringComparison.OrdinalIgnoreCase) == true;
}
