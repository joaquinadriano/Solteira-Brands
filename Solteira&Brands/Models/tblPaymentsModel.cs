using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblPaymentsModel
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; }
        public string RejectionReason { get; set; }
        public int? ProofImageId { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime? MeetupDatetime { get; set; }
        public int? VerifiedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public virtual tblOrdersModel Order { get; set; }
        public virtual tblImagesModel ProofImage { get; set; }
        public virtual tblUsersModel VerifiedByUser { get; set; }
    }
}
