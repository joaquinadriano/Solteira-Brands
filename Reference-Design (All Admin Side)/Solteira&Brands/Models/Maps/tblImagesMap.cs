using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblImagesMap : EntityTypeConfiguration<tblImagesModel>
    {
        public tblImagesMap()
        {
            ToTable("tbl_images");
            HasKey(x => x.Id);
            Property(x => x.ImagePath).HasColumnName("image_path").HasMaxLength(255).IsRequired();
            Property(x => x.ImageFileName).HasColumnName("image_file_name").HasMaxLength(255).IsRequired();
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}
