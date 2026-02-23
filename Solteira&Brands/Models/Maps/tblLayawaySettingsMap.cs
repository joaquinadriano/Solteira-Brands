using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblLayawaySettingsMap : EntityTypeConfiguration<tblLayawaySettingsModel>
    {
        public tblLayawaySettingsMap()
        {
            ToTable("tbl_layaway_settings");
            HasKey(x => x.Id);
            Property(x => x.Id).HasColumnName("id");
            Property(x => x.DownpaymentPercentage).HasColumnName("downpayment_percentage").HasPrecision(5, 2);
            Property(x => x.MonthsAllowed).HasColumnName("months_allowed").IsRequired();
            Property(x => x.ResubmitWindowDays).HasColumnName("resubmit_window_days").IsOptional();
            Property(x => x.GracePeriodDays).HasColumnName("grace_period_days").IsOptional();
            Property(x => x.CreatedAt).HasColumnName("created_at");
        }
    }
}
