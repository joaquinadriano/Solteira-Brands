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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Configurations.Add(new tblRolesMap());
            modelBuilder.Configurations.Add(new tblUsersMap());
            modelBuilder.Configurations.Add(new tblImagesMap());
            modelBuilder.Configurations.Add(new tblProductsMap());
            modelBuilder.Configurations.Add(new tblOrdersMap());
            modelBuilder.Configurations.Add(new tblOrderItemsMap());
        }
    }
}
