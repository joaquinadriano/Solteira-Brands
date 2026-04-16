using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    /// <summary>
    /// DTO for creating/updating products from admin inventory.
    /// </summary>
    public class ProductSaveRequest
    {
        public int? Id { get; set; }

        [StringLength(ProductFieldConstraints.MaxNameLength)]
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }

        [StringLength(ProductFieldConstraints.MaxSpecFieldLength)]
        public string Material { get; set; }

        [StringLength(30)]
        public string Color { get; set; }

        [StringLength(10)]
        public string RingSize { get; set; }

        [StringLength(ProductFieldConstraints.MaxSpecFieldLength)]
        public string Stones { get; set; }

        [StringLength(ProductFieldConstraints.MaxSpecFieldLength)]
        public string Weight { get; set; }

        [StringLength(ProductFieldConstraints.MaxSpecFieldLength)]
        public string Length { get; set; }
        public int StockQuantity { get; set; }
        public int? ImageId { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsFeatured { get; set; }
    }
}

