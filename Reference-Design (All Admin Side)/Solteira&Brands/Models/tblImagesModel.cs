using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblImagesModel
    {
        [Key]
        public int Id { get; set; }

        public string ImagePath { get; set; }
        public string ImageFileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
