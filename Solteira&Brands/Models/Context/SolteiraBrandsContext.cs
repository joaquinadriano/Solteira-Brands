using Solteira_Brands.Models;
using Solteira_Brands.Models.Maps;
using System.Data.Entity;

namespace Solteira_Brands.Models.Context
{
    public class SolteiraBrandsContext : DbContext
    {
        static SolteiraBrandsContext()
        {
            Database.SetInitializer<SolteiraBrandsContext>(null);
        }

        public SolteiraBrandsContext() : base("Name=SolteiraBrandsConnection") { }

        public virtual DbSet<tblRolesModel> tbl_roles { get; set; }
        public virtual DbSet<tblUsersModel> tbl_users { get; set; }
        public virtual DbSet<tblImagesModel> tbl_images { get; set; }
        public virtual DbSet<tblProductsModel> tbl_products { get; set; }
        public virtual DbSet<tblOrdersModel> tbl_orders { get; set; }
        public virtual DbSet<tblOrderItemsModel> tbl_order_items { get; set; }
        public virtual DbSet<tblPaymentsModel> tbl_payments { get; set; }
        public virtual DbSet<tblDeliveriesModel> tbl_deliveries { get; set; }
        public virtual DbSet<tblActivityLogModel> tbl_activity_logs { get; set; }
        public virtual DbSet<tblPaymentSettingsModel> tbl_payment_settings { get; set; }
        public virtual DbSet<tblMeetupSettingsModel> tbl_meetup_settings { get; set; }
        public virtual DbSet<tblLayawaySettingsModel> tbl_layaway_settings { get; set; }
        public virtual DbSet<tblMeetupOutcomeModel> tbl_meetup_outcomes { get; set; }
        public virtual DbSet<tblVerificationTokenModel> tbl_verification_tokens { get; set; }
        public virtual DbSet<tblCartsModel> tbl_carts { get; set; }
        public virtual DbSet<tblCartItemsModel> tbl_cart_items { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Configurations.Add(new tblRolesMap());
            modelBuilder.Configurations.Add(new tblUsersMap());
            modelBuilder.Configurations.Add(new tblImagesMap());
            modelBuilder.Configurations.Add(new tblProductsMap());
            modelBuilder.Configurations.Add(new tblOrdersMap());
            modelBuilder.Configurations.Add(new tblOrderItemsMap());
            modelBuilder.Configurations.Add(new tblPaymentsMap());
            modelBuilder.Configurations.Add(new tblDeliveriesMap());
            modelBuilder.Configurations.Add(new tblActivityLogMap());
            modelBuilder.Configurations.Add(new tblPaymentSettingsMap());
            modelBuilder.Configurations.Add(new tblMeetupSettingsMap());
            modelBuilder.Configurations.Add(new tblLayawaySettingsMap());
            modelBuilder.Configurations.Add(new tblMeetupOutcomeMap());
            modelBuilder.Configurations.Add(new tblVerificationTokenMap());
            modelBuilder.Configurations.Add(new tblCartsMap());
            modelBuilder.Configurations.Add(new tblCartItemsMap());
        }
    }
}
