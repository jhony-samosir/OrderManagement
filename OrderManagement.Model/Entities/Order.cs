using OrderManagement.Model.Enums;

namespace OrderManagement.Model.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public string ShippingAddress { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
}
