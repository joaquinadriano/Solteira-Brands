using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblCartsModel
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

