using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblPolicySettingsMap : EntityTypeConfiguration<tblPolicySettingsModel>
    {
        public tblPolicySettingsMap()
        {
            ToTable("tbl_policy_settings");
            HasKey(x => x.Id);
            Property(x => x.Id).HasColumnName("id");
            Property(x => x.PolicyType).HasColumnName("policy_type").HasMaxLength(50).IsRequired();
            Property(x => x.Content).HasColumnName("content").IsRequired();
            Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        }
    }
}
