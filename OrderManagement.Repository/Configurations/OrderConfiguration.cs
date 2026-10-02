using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OrderManagement.Model.Entities;

namespace OrderManagement.Repository.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd();

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.ShippingAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(o => o.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_Orders_IdempotencyKey");

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.TotalAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired();
        
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
