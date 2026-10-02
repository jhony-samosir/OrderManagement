using OrderManagement.Model.DTOs.Requests;
using OrderManagement.Model.DTOs.Responses;

namespace OrderManagement.Model.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken ct = default);
    Task<OrderResponse> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<PagedResponse<OrderResponse>> ListOrdersAsync(ListOrdersRequest request, CancellationToken ct = default);
    Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request, CancellationToken ct = default);
    Task<OrderResponse> CancelOrderAsync(Guid orderId, CancellationToken ct = default);
}
