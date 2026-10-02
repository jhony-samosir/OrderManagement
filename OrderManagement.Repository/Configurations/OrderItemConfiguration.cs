using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderManagement.Model.Entities;

namespace OrderManagement.Repository.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedOnAdd();

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.HasOne(i => i.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
