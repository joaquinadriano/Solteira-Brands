using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblProductsModel
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Material { get; set; }
        public string Stones { get; set; }
        public string Weight { get; set; }
        public string Length { get; set; }
        public int StockQuantity { get; set; }
        public int? ImageId { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

