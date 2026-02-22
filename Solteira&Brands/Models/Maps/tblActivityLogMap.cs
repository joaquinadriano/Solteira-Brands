using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblActivityLogMap : EntityTypeConfiguration<tblActivityLogModel>
    {
        public tblActivityLogMap()
        {
            ToTable("tbl_activity_logs");
            HasKey(x => x.Id);
            Property(x => x.UserId).HasColumnName("user_id");
            Property(x => x.Username).HasColumnName("username").HasMaxLength(150).IsRequired();
            Property(x => x.RoleName).HasColumnName("role_name").HasMaxLength(50).IsRequired();
            Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
            Property(x => x.Module).HasColumnName("module").HasMaxLength(50).IsRequired();
            Property(x => x.EntityId).HasColumnName("entity_id");
            Property(x => x.Details).HasColumnName("details").HasMaxLength(500).IsOptional();
            Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }
}
