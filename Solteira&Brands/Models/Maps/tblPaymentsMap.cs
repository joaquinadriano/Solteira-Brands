using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblPaymentsMap : EntityTypeConfiguration<tblPaymentsModel>
    {
        public tblPaymentsMap()
        {
            ToTable("tbl_payments");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("id");
            Property(x => x.OrderId).HasColumnName("order_id");
            Property(x => x.Amount).HasColumnName("amount");
            Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(50).IsRequired();
            Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
            Property(x => x.ProofImageId).HasColumnName("proof_image_id");
            Property(x => x.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100).IsOptional();
            Property(x => x.MeetupDatetime).HasColumnName("meetup_datetime").IsOptional();
            Property(x => x.VerifiedByUserId).HasColumnName("verified_by_user_id");
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.VerifiedAt).HasColumnName("verified_at");
            Property(x => x.AllowResubmit).HasColumnName("allow_resubmit");

            HasRequired(x => x.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(x => x.OrderId)
                .WillCascadeOnDelete(true);

            HasOptional(x => x.ProofImage)
                .WithMany()
                .HasForeignKey(x => x.ProofImageId)
                .WillCascadeOnDelete(false);

            HasOptional(x => x.VerifiedByUser)
                .WithMany()
                .HasForeignKey(x => x.VerifiedByUserId)
                .WillCascadeOnDelete(false);
        }
    }
}
