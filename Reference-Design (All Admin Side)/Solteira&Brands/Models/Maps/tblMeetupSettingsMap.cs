using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblMeetupSettingsMap : EntityTypeConfiguration<tblMeetupSettingsModel>
    {
        public tblMeetupSettingsMap()
        {
            ToTable("tbl_meetup_settings");
            HasKey(x => x.Id);
            Property(x => x.Id).HasColumnName("id");
            Property(x => x.LocationName).HasColumnName("location_name").HasMaxLength(150).IsRequired();
            Property(x => x.Address).HasColumnName("address").HasMaxLength(500).IsRequired();
            Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }
}
