using Microsoft.EntityFrameworkCore;
using OrderManagement.Model.DTOs.Requests;
using OrderManagement.Model.Entities;
using OrderManagement.Model.Interfaces;

namespace OrderManagement.Repository.Repositories;

public class OrderRepository(AppDbContext context) : IOrderRepository
{
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Orders.FindAsync([id], ct);

    public async Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        => await context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
        => await context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, ct);

    public async Task<(List<Order> Items, int TotalCount)> ListAsync(
        ListOrdersRequest request,
        CancellationToken ct = default)
    {
        var query = context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        if (request.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);

        if (request.DateFrom.HasValue)
            query = query.Where(o => o.CreatedAt >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(o => o.CreatedAt <= request.DateTo.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
        => await context.Orders.AddAsync(order, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
