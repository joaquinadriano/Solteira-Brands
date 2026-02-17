using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblRolesModel
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<tblUsersModel> Users { get; set; }
    }
}
