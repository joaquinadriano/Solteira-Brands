using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblProductsMap : EntityTypeConfiguration<tblProductsModel>
    {
        public tblProductsMap()
        {
            ToTable("tbl_products");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("id");
            Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            Property(x => x.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
            Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            Property(x => x.Price).HasColumnName("price");
            Property(x => x.Material).HasColumnName("material").HasMaxLength(150);
            Property(x => x.Stones).HasColumnName("stones").HasMaxLength(255);
            Property(x => x.Weight).HasColumnName("weight").HasMaxLength(50);
            Property(x => x.Length).HasColumnName("length").HasMaxLength(50);
            Property(x => x.StockQuantity).HasColumnName("stock_quantity");
            Property(x => x.ImageId).HasColumnName("image_id");
            Property(x => x.IsBestSeller).HasColumnName("is_best_seller");
            Property(x => x.IsFeatured).HasColumnName("is_featured");
            Property(x => x.IsArchived).HasColumnName("is_archived");
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}

