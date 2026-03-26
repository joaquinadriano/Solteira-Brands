using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblPaymentSettingsMap : EntityTypeConfiguration<tblPaymentSettingsModel>
    {
        public tblPaymentSettingsMap()
        {
            ToTable("tbl_payment_settings");
            HasKey(x => x.Id);
            Property(x => x.Id).HasColumnName("id");
            Property(x => x.PaymentType).HasColumnName("payment_type").HasMaxLength(50).IsRequired();
            Property(x => x.AccountName).HasColumnName("account_name").HasMaxLength(150).IsOptional();
            Property(x => x.AccountNumber).HasColumnName("account_number").HasMaxLength(100).IsOptional();
            Property(x => x.QrImageId).HasColumnName("qr_image_id");
            Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.UpdatedAt).HasColumnName("updated_at");

            HasOptional(x => x.QrImage)
                .WithMany()
                .HasForeignKey(x => x.QrImageId)
                .WillCascadeOnDelete(false);
        }
    }
}
