using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblUsersMap : EntityTypeConfiguration<tblUsersModel>
    {
        public tblUsersMap()
        {
            ToTable("tbl_users");
            HasKey(x => x.Id);

            HasRequired(x => x.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(x => x.RoleId)
                .WillCascadeOnDelete(false);
        }
    }
}
