using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderManagement.Model.DTOs.Requests;
using OrderManagement.Model.Entities;
using OrderManagement.Model.Enums;
using OrderManagement.Model.Interfaces;
using OrderManagement.Service.Exceptions;
using OrderManagement.Service;
using Xunit;

namespace OrderManagement.Tests.Unit;

public sealed class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(
            _orderRepoMock.Object,
            _productRepoMock.Object,
            NullLogger<OrderService>.Instance);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WhenOrderExists_ReturnsOrderResponse()
    {
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, OrderStatus.Pending);
        _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(orderId, default)).ReturnsAsync(order);

        var result = await _sut.GetOrderByIdAsync(orderId);

        result.Id.Should().Be(orderId);
        result.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Order?)null);

        var act = () => _sut.GetOrderByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*not found*");
    }

    

    [Fact]
    public async Task CreateOrderAsync_WhenProductNotFound_ThrowsNotFoundException()
    {
        var request = CreateOrderRequest();
        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default)).ReturnsAsync([]);   

        var act = () => _sut.CreateOrderAsync(request, Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateOrderAsync_WhenStockInsufficient_ThrowsInsufficientStockException_AndRestoresNothingDeducted()
    {
        var productId = Guid.NewGuid();
        var request = CreateOrderRequest(productId);
        var product = new Product { Id = productId, Name = "Widget", Price = 10m, StockQuantity = 0 };

        _productRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default)).ReturnsAsync([product]);
        _productRepoMock.Setup(r => r.TryDeductStockAsync(productId, It.IsAny<int>(), default)).ReturnsAsync(false);   

        var act = () => _sut.CreateOrderAsync(request, Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<InsufficientStockException>().WithMessage("*Widget*");
                
        _productRepoMock.Verify(r => r.RestoreStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), default), Times.Never);
    }        

    [Fact]
    public async Task UpdateOrderStatusAsync_WithValidTransition_UpdatesStatus()
    {
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, OrderStatus.Pending);
        _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(orderId, default)).ReturnsAsync(order);

        var result = await _sut.UpdateOrderStatusAsync(
            orderId,
            new UpdateOrderStatusRequest { Status = OrderStatus.Confirmed });

        result.Status.Should().Be(OrderStatus.Confirmed);
        _orderRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WithInvalidTransition_ThrowsInvalidStatusTransitionException()
    {
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, OrderStatus.Delivered);   
        _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(orderId, default)).ReturnsAsync(order);

        var act = () => _sut.UpdateOrderStatusAsync(
            orderId,
            new UpdateOrderStatusRequest { Status = OrderStatus.Cancelled });

        await act.Should().ThrowAsync<InvalidStatusTransitionException>()
            .WithMessage("*Delivered*Cancelled*");
    }    

    [Fact]
    public async Task CancelOrderAsync_WhenPending_CancelsAndRestoresStock()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, OrderStatus.Pending);
        order.Items.Add(new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = 3,
            UnitPrice = 10m,
            Product = new Product { Id = productId, Name = "Widget", Price = 10m, StockQuantity = 10 }
        });

        _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(orderId, default)).ReturnsAsync(order);

        var result = await _sut.CancelOrderAsync(orderId);

        result.Status.Should().Be(OrderStatus.Cancelled);
        _productRepoMock.Verify(r => r.RestoreStockAsync(productId, 3, default), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenDelivered_ThrowsInvalidStatusTransitionException()
    {
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, OrderStatus.Delivered);   
        _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(orderId, default)).ReturnsAsync(order);

        var act = () => _sut.CancelOrderAsync(orderId);

        await act.Should().ThrowAsync<InvalidStatusTransitionException>()
            .WithMessage("*Delivered*Cancelled*");

        _productRepoMock.Verify(r => r.RestoreStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), default), Times.Never);
    }    

    private static Order CreateOrder(Guid id, OrderStatus status) => new()
    {
        Id = id,
        CustomerId = Guid.NewGuid(),
        Status = status,
        ShippingAddress = "123 Test St",
        IdempotencyKey = Guid.NewGuid().ToString(),
        TotalAmount = 100m,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
        Items = []
    };

    private static CreateOrderRequest CreateOrderRequest(Guid? productId = null) => new()
    {
        CustomerId = Guid.NewGuid(),
        ShippingAddress = "123 Test St",
        Items = [new OrderItemRequest { ProductId = productId ?? Guid.NewGuid(), Quantity = 2 }]
    };
}
