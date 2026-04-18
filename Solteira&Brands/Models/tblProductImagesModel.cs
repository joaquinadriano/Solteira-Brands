using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblProductImagesModel
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ImageId { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
