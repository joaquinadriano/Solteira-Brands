using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblOrdersModel
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string ContactNumber { get; set; }
        public string DeliveryAddress { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RemainingBalance { get; set; }
        public string OrderStatus { get; set; }
        public string DeliveryType { get; set; }
        public string RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastStatusUpdateAt { get; set; }
        public bool IsArchived { get; set; }

        public virtual tblUsersModel User { get; set; }
        public virtual ICollection<tblOrderItemsModel> OrderItems { get; set; }
        public virtual ICollection<tblPaymentsModel> Payments { get; set; }
        public virtual ICollection<tblDeliveriesModel> Deliveries { get; set; }
    }
}
