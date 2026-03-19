using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblCartItemsModel
    {
        [Key]
        public int Id { get; set; }

        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

