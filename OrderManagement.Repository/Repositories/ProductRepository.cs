using Microsoft.EntityFrameworkCore;
using OrderManagement.Model.Entities;
using OrderManagement.Model.Interfaces;

namespace OrderManagement.Repository.Repositories;

public class ProductRepository(AppDbContext context) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Products.FindAsync([id], ct);

    public async Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        => await context.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);

    public async Task<bool> TryDeductStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        var affected = await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE products
            SET stock_quantity = stock_quantity - {0}
            WHERE id = {1} AND stock_quantity >= {2}
            """,
            new object[] { quantity, productId, quantity }, ct);

        return affected > 0;
    }

    public async Task RestoreStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE products
            SET stock_quantity = stock_quantity + {0}
            WHERE id = {1}
            """,
            new object[] { quantity, productId }, ct);
    }
}
