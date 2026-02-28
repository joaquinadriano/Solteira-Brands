using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblMeetupOutcomeMap : EntityTypeConfiguration<tblMeetupOutcomeModel>
    {
        public tblMeetupOutcomeMap()
        {
            ToTable("tbl_meetup_outcomes");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("id");
            Property(x => x.OrderId).HasColumnName("order_id");
            Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            Property(x => x.ReceiptImageId).HasColumnName("receipt_image_id");
            Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500).IsOptional();
            Property(x => x.RescheduledAt).HasColumnName("rescheduled_at").IsOptional();
            Property(x => x.CreatedAt).HasColumnName("created_at");

            HasRequired(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .WillCascadeOnDelete(true);

            HasOptional(x => x.ReceiptImage)
                .WithMany()
                .HasForeignKey(x => x.ReceiptImageId)
                .WillCascadeOnDelete(false);
        }
    }
}
