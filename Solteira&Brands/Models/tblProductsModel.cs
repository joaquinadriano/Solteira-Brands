using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblProductsModel
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(ProductFieldConstraints.MaxNameLength)]
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        [MaxLength(ProductFieldConstraints.MaxSpecFieldLength)]
        public string Material { get; set; }

        [MaxLength(10)]
        public string RingSize { get; set; }

        [MaxLength(ProductFieldConstraints.MaxSpecFieldLength)]
        public string Stones { get; set; }

        [MaxLength(ProductFieldConstraints.MaxSpecFieldLength)]
        public string Weight { get; set; }

        [MaxLength(ProductFieldConstraints.MaxSpecFieldLength)]
        public string Length { get; set; }
        public int StockQuantity { get; set; }
        public int? InitialStock { get; set; }
        public int? ImageId { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

