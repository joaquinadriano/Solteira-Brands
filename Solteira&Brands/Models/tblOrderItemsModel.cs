using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblOrderItemsModel
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ImageUrl { get; set; }

        public virtual tblOrdersModel Order { get; set; }
    }
}
