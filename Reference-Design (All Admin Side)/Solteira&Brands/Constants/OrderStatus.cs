namespace Solteira_Brands.Constants
{
    /// <summary>Centralized order status values. Use these instead of string literals.</summary>
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        /// <summary>Order placed; waiting for admin to Approve or Reject. No payment allowed until Approved.</summary>
        public const string PendingApproval = "Pending Approval";
        public const string PendingPayment = "Pending Payment";
        public const string PendingDownpayment = "Pending Downpayment";
        public const string Approved = "Approved";
        public const string AwaitingPaymentVerification = "Awaiting Payment Verification";
        public const string AwaitingMeetup = "Awaiting Meetup";
        public const string LayawayActive = "Layaway Active";
        public const string Paid = "Paid";
        public const string InDelivery = "In Delivery";
        public const string ReadyForMeetup = "Ready for Meetup";
        public const string NoShow = "No Show";
        public const string MeetupRescheduled = "Meetup Rescheduled";
        public const string Completed = "Completed";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
    }
}
