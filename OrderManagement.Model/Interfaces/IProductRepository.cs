using OrderManagement.Model.Entities;

namespace OrderManagement.Model.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<bool> TryDeductStockAsync(Guid productId, int quantity, CancellationToken ct = default);
    Task RestoreStockAsync(Guid productId, int quantity, CancellationToken ct = default);
}
