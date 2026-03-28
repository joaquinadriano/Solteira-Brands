using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblUsersMap : EntityTypeConfiguration<tblUsersModel>
    {
        public tblUsersMap()
        {
            ToTable("tbl_users");
            HasKey(x => x.Id);
            Property(x => x.RoleId).HasColumnName("role_id");
            Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(50).IsRequired();
            Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(50).IsRequired();
            Property(x => x.Username).HasColumnName("username").HasMaxLength(150).IsRequired();
            Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            Property(x => x.PasswordSalt).HasColumnName("password_salt").HasMaxLength(255).IsRequired();
            Property(x => x.IsVerified).HasColumnName("is_verified");
            Property(x => x.IsActive).HasColumnName("is_active");
            Property(x => x.CreatedAt).HasColumnName("created_at");
            Property(x => x.UsernameChangedAt).HasColumnName("username_changed_at");
            Property(x => x.AcceptedTerms).HasColumnName("accepted_terms").IsRequired();
            Property(x => x.AcceptedAt).HasColumnName("accepted_at");
            Property(x => x.AvatarImageId).HasColumnName("avatar_image_id");

            HasRequired(x => x.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(x => x.RoleId)
                .WillCascadeOnDelete(false);

            HasOptional(x => x.AvatarImage)
                .WithMany()
                .HasForeignKey(x => x.AvatarImageId);
        }
    }
}
