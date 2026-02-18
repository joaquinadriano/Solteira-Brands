using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblOrderItemsMap : EntityTypeConfiguration<tblOrderItemsModel>
    {
        public tblOrderItemsMap()
        {
            ToTable("tbl_order_items");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("id");
            Property(x => x.OrderId).HasColumnName("order_id");
            Property(x => x.ProductId).HasColumnName("product_id");
            Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
            Property(x => x.Quantity).HasColumnName("quantity");
            Property(x => x.UnitPrice).HasColumnName("unit_price");
            Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);

            HasRequired(x => x.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .WillCascadeOnDelete(true);
        }
    }
}
