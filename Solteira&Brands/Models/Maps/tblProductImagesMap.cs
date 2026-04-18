using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblProductImagesMap : EntityTypeConfiguration<tblProductImagesModel>
    {
        public tblProductImagesMap()
        {
            ToTable("tbl_product_images");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("id");
            Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
            Property(x => x.ImageId).HasColumnName("image_id").IsRequired();
            Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}
