using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OrderManagement.Model.Entities;

namespace OrderManagement.Repository.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Price)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(p => p.StockQuantity)
            .IsRequired();

        
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Products_StockQuantity_NonNegative",
            "stock_quantity >= 0"));
    }
}
