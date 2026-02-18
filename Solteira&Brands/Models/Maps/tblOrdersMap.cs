using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblOrdersMap : EntityTypeConfiguration<tblOrdersModel>
    {
        public tblOrdersMap()
        {
            ToTable("tbl_orders");
            HasKey(x => x.Id);

            Property(x => x.Id).HasColumnName("id");
            Property(x => x.UserId).HasColumnName("user_id");
            Property(x => x.OrderNumber).HasColumnName("order_number").HasMaxLength(50).IsRequired();
            Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(150).IsRequired();
            Property(x => x.ContactNumber).HasColumnName("contact_number").HasMaxLength(50).IsRequired();
            Property(x => x.DeliveryAddress).HasColumnName("delivery_address").HasMaxLength(500).IsRequired();
            Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(100).IsRequired();
            Property(x => x.TotalAmount).HasColumnName("total_amount");
            Property(x => x.OrderStatus).HasColumnName("order_status").HasMaxLength(50).IsRequired();
            Property(x => x.CreatedAt).HasColumnName("created_at");

            HasRequired(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .WillCascadeOnDelete(false);

            HasMany(x => x.OrderItems)
                .WithRequired(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .WillCascadeOnDelete(true);
        }
    }
}
