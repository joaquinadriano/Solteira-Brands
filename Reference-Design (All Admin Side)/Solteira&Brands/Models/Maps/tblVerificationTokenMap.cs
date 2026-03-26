using System.Data.Entity.ModelConfiguration;

namespace Solteira_Brands.Models.Maps
{
    public class tblVerificationTokenMap : EntityTypeConfiguration<tblVerificationTokenModel>
    {
        public tblVerificationTokenMap()
        {
            ToTable("tbl_verification_tokens");
            HasKey(x => x.Id);
            Property(x => x.Id).HasColumnName("id");
            Property(x => x.UserId).HasColumnName("user_id");
            Property(x => x.TokenValue).HasColumnName("token_value").HasMaxLength(255).IsRequired();
            Property(x => x.TokenType).HasColumnName("token_type").HasMaxLength(50).IsRequired();
            Property(x => x.ExpiresAt).HasColumnName("expires_at");
            Property(x => x.IsUsed).HasColumnName("is_used");
            Property(x => x.CreatedAt).HasColumnName("created_at");

            HasRequired(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .WillCascadeOnDelete(true);
        }
    }
}
