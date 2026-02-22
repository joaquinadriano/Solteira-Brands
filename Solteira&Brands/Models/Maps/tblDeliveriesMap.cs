using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblDeliveriesMap : EntityTypeConfiguration<tblDeliveriesModel>
    {
        public tblDeliveriesMap()
        {
            ToTable("tbl_deliveries");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("id");
            Property(x => x.OrderId).HasColumnName("order_id");
            Property(x => x.DeliveryType).HasColumnName("delivery_type").HasMaxLength(50).IsRequired();
            Property(x => x.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(200);
            Property(x => x.MeetupDatetime).HasColumnName("meetup_datetime");
            Property(x => x.DeliveryStatus).HasColumnName("delivery_status").HasMaxLength(50).IsRequired();
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.UpdatedAt).HasColumnName("updated_at");

            HasRequired(x => x.Order)
                .WithMany(o => o.Deliveries)
                .HasForeignKey(x => x.OrderId)
                .WillCascadeOnDelete(true);
        }
    }
}
