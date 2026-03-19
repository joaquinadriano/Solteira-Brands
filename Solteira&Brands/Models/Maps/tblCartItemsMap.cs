using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblCartItemsMap : EntityTypeConfiguration<tblCartItemsModel>
    {
        public tblCartItemsMap()
        {
            ToTable("tbl_cart_items");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("id");
            Property(x => x.CartId).HasColumnName("cart_id");
            Property(x => x.ProductId).HasColumnName("product_id");
            Property(x => x.Quantity).HasColumnName("quantity");
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}

