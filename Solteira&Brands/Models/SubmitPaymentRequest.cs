namespace Solteira_Brands.Models
{
    /// <summary>
    /// Request body for submitting payment (same pattern as avatar/QR: upload first, then submit with image ID).
    /// </summary>
    public class SubmitPaymentRequest
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public int? ProofImageId { get; set; }
        public string ReferenceNumber { get; set; }
        public string MeetupDate { get; set; }
        public string MeetupTime { get; set; }
        public string PaymentChannel { get; set; }
    }
}
