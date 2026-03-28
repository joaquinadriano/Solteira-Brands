using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblUsersModel
    {
        [Key]
        public int Id { get; set; }

        public int RoleId { get; set; }
        public string Email { get; set; }
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-Z\s'\.-]+$")]
        public string FirstName { get; set; }
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-Z\s'\.-]+$")]
        public string LastName { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        // When the username was last changed (UTC). Used for the 30-day username cooldown.
        public DateTime? UsernameChangedAt { get; set; }
        public bool AcceptedTerms { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public int? AvatarImageId { get; set; }

        public virtual tblRolesModel Role { get; set; }
        public virtual tblImagesModel AvatarImage { get; set; }
    }
}
