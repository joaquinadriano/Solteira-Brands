using System;
using System.ComponentModel.DataAnnotations;

namespace Solteira_Brands.Models
{
    public class tblMeetupOutcomeModel
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public string Status { get; set; }
        public int? ReceiptImageId { get; set; }
        public string Reason { get; set; }
        public DateTime? RescheduledAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual tblOrdersModel Order { get; set; }
        public virtual tblImagesModel ReceiptImage { get; set; }
    }
}
