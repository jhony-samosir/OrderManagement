using Microsoft.EntityFrameworkCore;
using OrderManagement.Model.Entities;
using OrderManagement.Repository.Configurations;

namespace OrderManagement.Repository;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
