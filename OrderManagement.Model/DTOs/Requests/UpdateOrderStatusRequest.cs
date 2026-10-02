using System.ComponentModel.DataAnnotations;
using OrderManagement.Model.Enums;

namespace OrderManagement.Model.DTOs.Requests;

public sealed record UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus Status { get; init; }
}
