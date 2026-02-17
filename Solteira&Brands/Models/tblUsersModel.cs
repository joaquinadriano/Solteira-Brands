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
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? AvatarImageId { get; set; }

        public virtual tblRolesModel Role { get; set; }
        public virtual tblImagesModel AvatarImage { get; set; }
    }
}
