using OrderManagement.Model.Enums;

namespace OrderManagement.Model.DTOs.Requests;

public sealed record ListOrdersRequest
{
    public OrderStatus? Status { get; init; }
    public Guid? CustomerId { get; init; }
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
