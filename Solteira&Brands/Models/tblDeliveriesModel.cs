using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblDeliveriesModel
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public string DeliveryType { get; set; }
        public string TrackingNumber { get; set; }
        public DateTime? MeetupDatetime { get; set; }
        public string DeliveryStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual tblOrdersModel Order { get; set; }
    }
}
