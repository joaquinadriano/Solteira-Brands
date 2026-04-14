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

        [StringLength(500)]
        public string DeliveryAddress { get; set; }

        [StringLength(500)]
        public string BillingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RemainingBalance { get; set; }
        public string OrderStatus { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        // Used to prevent spamming reminder emails for the same payment deadline.
        public DateTime? PaymentDeadlineReminderSentFor { get; set; }
        public DateTime? PaymentDeadlineReminderSentAt { get; set; }
        public string CancellationReason { get; set; }
        // When false: stock is currently deducted/reserved for this order and has NOT been restored yet.
        // When true: stock restoration has already happened (prevents double-restoration).
        public bool InventoryReleased { get; set; }
        // Convenience flag for deadline/layaway behavior.
        public bool IsLayaway { get; set; }
        public string DeliveryType { get; set; }
        public DateTime? MeetupDatetime { get; set; }
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
