using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblRolesMap : EntityTypeConfiguration<tblRolesModel>
    {
        public tblRolesMap()
        {
            ToTable("tbl_roles");
            HasKey(x => x.Id);
            Property(x => x.Name).HasColumnName("name").HasMaxLength(50);
            Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
            Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }
}
