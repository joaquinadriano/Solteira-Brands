using System;
using Solteira_Brands.Constants;

namespace Solteira_Brands.Services
{
    /// <summary>
    /// Centralized lifecycle rules for orders, payments, and deliveries.
    /// All status changes should go through these helpers so that
    /// invalid transitions are blocked and states stay consistent.
    /// </summary>
    public static class OrderLifecycleRules
    {
        // ----- ORDER STATUS -----

        /// <summary>
        /// Returns true when transitioning from <paramref name="from"/> to <paramref name="to"/> is allowed.
        /// Uses the string-backed OrderStatus constants (database stores human-readable values).
        /// </summary>
        public static bool CanTransitionOrder(string from, string to)
        {
            var current = NormalizeOrder(from);
            var target = NormalizeOrder(to);
            if (current == target)
                return true;

            switch (current)
            {
                case null:
                    // New order: must start at Pending Approval.
                    return target == OrderStatus.PendingApproval;

                case OrderStatus.PendingApproval:
                    // Order approval: admin Approve or Reject only. No payment until Approved.
                    return target == OrderStatus.Approved
                        || target == OrderStatus.Rejected
                        || target == OrderStatus.Cancelled;

                case OrderStatus.Pending:
                    return target == OrderStatus.Approved
                        || target == OrderStatus.Rejected
                        || target == OrderStatus.Cancelled;

                case OrderStatus.PendingPayment:
                    // Full payment: buyer can submit payment or admin cancels/rejects.
                    return target == OrderStatus.AwaitingPaymentVerification
                        || target == OrderStatus.Approved
                        || target == OrderStatus.Rejected
                        || target == OrderStatus.Cancelled;

                case OrderStatus.PendingDownpayment:
                    // Layaway: buyer can submit downpayment, or admin approves/rejects/cancels.
                    return target == OrderStatus.AwaitingPaymentVerification
                        || target == OrderStatus.Approved
                        || target == OrderStatus.Rejected
                        || target == OrderStatus.Cancelled;

                case OrderStatus.Approved:
                    // Payment flow starts here; admin can revoke (back to Pending Approval) or reject/cancel.
                    return target == OrderStatus.AwaitingPaymentVerification
                        || target == OrderStatus.PendingApproval
                        || target == OrderStatus.Rejected
                        || target == OrderStatus.Cancelled;

                case OrderStatus.AwaitingPaymentVerification:
                    // Result of reviewing a payment.
                    return target == OrderStatus.Paid
                        || target == OrderStatus.ReadyForMeetup
                        || target == OrderStatus.LayawayActive
                        || target == OrderStatus.PendingPayment   // payment rejected (full)
                        || target == OrderStatus.PendingDownpayment  // payment rejected (layaway downpayment)
                        || target == OrderStatus.Approved         // legacy: payment rejected
                        || target == OrderStatus.Cancelled;

                case OrderStatus.LayawayActive:
                    // Layaway: submit next installment → Awaiting Verification, or fully paid/cancelled.
                    return target == OrderStatus.AwaitingPaymentVerification
                        || target == OrderStatus.Paid
                        || target == OrderStatus.ReadyForMeetup
                        || target == OrderStatus.Cancelled;

                case OrderStatus.Paid:
                    // After full payment, order can move into delivery/meetup flows or be cancelled.
                    return target == OrderStatus.InDelivery
                        || target == OrderStatus.ReadyForMeetup
                        || target == OrderStatus.Completed
                        || target == OrderStatus.Cancelled;

                case OrderStatus.InDelivery:
                    return target == OrderStatus.Completed
                        || target == OrderStatus.Cancelled;

                case OrderStatus.ReadyForMeetup:
                    return target == OrderStatus.Completed
                        || target == OrderStatus.NoShow
                        || target == OrderStatus.Cancelled;

                case OrderStatus.NoShow:
                    return target == OrderStatus.MeetupRescheduled;

                case OrderStatus.MeetupRescheduled:
                    return target == OrderStatus.Completed
                        || target == OrderStatus.NoShow
                        || target == OrderStatus.Cancelled;

                // Terminal states: no automatic transitions out.
                case OrderStatus.Completed:
                case OrderStatus.Rejected:
                case OrderStatus.Cancelled:
                    return false;

                default:
                    return false;
            }
        }

        private static string NormalizeOrder(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        }

        // ----- PAYMENT STATUS -----

        /// <summary>
        /// Returns true when transitioning from <paramref name="from"/> to <paramref name="to"/> is allowed.
        /// Payment statuses are upper-case tokens stored in tbl_payments.payment_status.
        /// </summary>
        public static bool CanTransitionPayment(string from, string to)
        {
            var current = NormalizeUpper(from);
            var target = NormalizeUpper(to);

            if (current == target)
                return true;

            if (current == null)
            {
                // New payment row must start as SUBMITTED (under review).
                return target == PaymentStatus.Submitted;
            }

            if (current == PaymentStatus.Submitted || current == PaymentStatus.AwaitingVerification)
            {
                // Under review → outcome.
                return target == PaymentStatus.Verified
                    || target == PaymentStatus.Rejected
                    || target == PaymentStatus.Expired;
            }

            // Verified / Rejected / Expired are terminal from an admin UI perspective.
            return false;
        }

        /// <summary>
        /// Helper for UI/business logic: true when a payment is "under review".
        /// </summary>
        public static bool IsPaymentPendingReview(string status)
        {
            var s = NormalizeUpper(status);
            return s == PaymentStatus.Submitted || s == PaymentStatus.AwaitingVerification;
        }

        // ----- DELIVERY STATUS -----

        /// <summary>
        /// Delivery transitions: any status allowed.
        /// </summary>
        public static bool CanTransitionDelivery(string from, string to)
        {
            var target = NormalizeUpper(to);
            var validStatuses = new[] { DeliveryStatus.Pending, DeliveryStatus.InTransit, DeliveryStatus.Delivered, DeliveryStatus.Failed };

            switch (target)
            {
                case DeliveryStatus.Pending:
                case DeliveryStatus.InTransit:
                case DeliveryStatus.Delivered:
                case DeliveryStatus.Failed:
                    return true;

                default:
                    return false;
            }
        }

        private static string NormalizeUpper(string status)
        {
            return string.IsNullOrWhiteSpace(status)
                ? null
                : status.Trim().ToUpperInvariant();
        }
    }
}

