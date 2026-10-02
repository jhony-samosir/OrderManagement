using OrderManagement.Model.Enums;

namespace OrderManagement.Model.DTOs.Responses;

public sealed record OrderResponse
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public OrderStatus Status { get; init; }
    public string StatusLabel => Status.ToString();
    public string ShippingAddress { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public List<OrderItemResponse> Items { get; init; } = [];
}

public sealed record OrderItemResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal => Quantity * UnitPrice;
}
