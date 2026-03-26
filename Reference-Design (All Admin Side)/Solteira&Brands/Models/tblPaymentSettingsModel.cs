using System;

namespace Solteira_Brands.Models
{
    public class tblPaymentSettingsModel
    {
        public int Id { get; set; }
        public string PaymentType { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public int? QrImageId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual tblImagesModel QrImage { get; set; }
    }
}
