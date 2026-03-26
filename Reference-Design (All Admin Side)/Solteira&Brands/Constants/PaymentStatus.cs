namespace Solteira_Brands.Constants
{
    /// <summary>Centralized payment/installment status values.</summary>
    public static class PaymentStatus
    {
        public const string Pending = "PENDING";
        public const string AwaitingVerification = "AWAITING_VERIFICATION";
        public const string Submitted = "SUBMITTED";
        public const string Verified = "VERIFIED";
        public const string Rejected = "REJECTED";
        public const string Expired = "EXPIRED";
    }
}
