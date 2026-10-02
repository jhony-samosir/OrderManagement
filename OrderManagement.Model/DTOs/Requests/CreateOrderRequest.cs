using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Model.DTOs.Requests;

public sealed record CreateOrderRequest
{
    [Required]
    public Guid CustomerId { get; init; }

    [Required, MinLength(1)]
    public string ShippingAddress { get; init; } = null!;

    [Required, MinLength(1)]
    public List<OrderItemRequest> Items { get; init; } = [];
}

public sealed record OrderItemRequest
{
    [Required]
    public Guid ProductId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; init; }
}
