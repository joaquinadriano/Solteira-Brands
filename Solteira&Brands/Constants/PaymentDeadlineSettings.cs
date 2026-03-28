namespace Solteira_Brands.Constants
{
    /// <summary>
    /// Central payment-deadline durations (UTC) and reminder lead times for scheduled emails.
    /// </summary>
    public static class PaymentDeadlineSettings
    {
        /// <summary>
        /// Days the buyer has to pay after admin approves the order, or after a rejected payment when resubmit is allowed (non-meetup).
        /// </summary>
        public const int DaysToPayAfterApprovalOrResubmit = 3;

        /// <summary>
        /// Layaway: extra days after the monthly due anchor when resetting deadline after a rejected installment (resubmit path).
        /// </summary>
        public const int LayawayResubmitGraceDaysAfterMonthlyDue = 3;

        /// <summary>
        /// Non-layaway orders: send one reminder email when the deadline falls in a short window around "now + this many days"
        /// (i.e. roughly this many days of time remaining before the deadline). Job runs every 5 minutes with ±30 min tolerance.
        /// </summary>
        public const int ReminderLeadDaysFullPayment = 1;

        /// <summary>
        /// Layaway installment deadlines: same pattern as full payment but with a longer lead.
        /// </summary>
        public const int ReminderLeadDaysLayaway = 2;
    }
}
