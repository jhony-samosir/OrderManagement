using OrderManagement.Model.DTOs.Requests;
using OrderManagement.Model.DTOs.Responses;
using OrderManagement.Model.Entities;

namespace OrderManagement.Model.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
    Task<(List<Order> Items, int TotalCount)> ListAsync(ListOrdersRequest request, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
