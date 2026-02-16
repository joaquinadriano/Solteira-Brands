using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblRolesMap : EntityTypeConfiguration<tblRolesModel>
    {
        public tblRolesMap()
        {
            ToTable("tbl_roles");
            HasKey(x => x.Id);
        }
    }
}
