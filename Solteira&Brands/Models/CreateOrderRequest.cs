using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    /// <summary>
    /// DTO for creating an order from checkout.
    /// </summary>
    public class CreateOrderRequest
    {
        public string CustomerName { get; set; }
        public string ContactNumber { get; set; }

        [StringLength(500)]
        public string DeliveryAddress { get; set; }

        [StringLength(500)]
        public string BillingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CreateOrderItemRequest> Items { get; set; }
    }

    public class CreateOrderItemRequest
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ImageUrl { get; set; }
    }
}
