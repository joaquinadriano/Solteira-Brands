using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblCartsMap : EntityTypeConfiguration<tblCartsModel>
    {
        public tblCartsMap()
        {
            ToTable("tbl_carts");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("id");
            Property(x => x.UserId).HasColumnName("user_id");
            Property(x => x.IsActive).HasColumnName("is_active");
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}

