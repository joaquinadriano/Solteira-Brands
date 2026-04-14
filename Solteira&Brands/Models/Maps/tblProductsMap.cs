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
            Property(x => x.Name).HasColumnName("name").HasMaxLength(Solteira_Brands.Models.ProductFieldConstraints.MaxNameLength).IsRequired();
            Property(x => x.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
            Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            Property(x => x.Price).HasColumnName("price");
            Property(x => x.Material).HasColumnName("material").HasMaxLength(Solteira_Brands.Models.ProductFieldConstraints.MaxSpecFieldLength);
            Property(x => x.RingSize).HasColumnName("ring_size").HasMaxLength(10);
            Property(x => x.Stones).HasColumnName("stones").HasMaxLength(Solteira_Brands.Models.ProductFieldConstraints.MaxSpecFieldLength);
            Property(x => x.Weight).HasColumnName("weight").HasMaxLength(Solteira_Brands.Models.ProductFieldConstraints.MaxSpecFieldLength);
            Property(x => x.Length).HasColumnName("length").HasMaxLength(Solteira_Brands.Models.ProductFieldConstraints.MaxSpecFieldLength);
            Property(x => x.StockQuantity).HasColumnName("stock_quantity");
            Property(x => x.InitialStock).HasColumnName("initial_stock");
            Property(x => x.ImageId).HasColumnName("image_id");
            Property(x => x.IsBestSeller).HasColumnName("is_best_seller");
            Property(x => x.IsFeatured).HasColumnName("is_featured");
            Property(x => x.IsArchived).HasColumnName("is_archived");
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}

