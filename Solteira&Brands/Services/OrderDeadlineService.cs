using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using Solteira_Brands.Constants;
using Solteira_Brands.Helpers;
using Solteira_Brands.Models;
using Solteira_Brands.Models.Context;

namespace Solteira_Brands.Services
{
    public static class OrderDeadlineService
    {
        private static Timer _timer;
        private static bool _started;
        private static int _isRunning;

        public static void StartAutoCancellationJob()
        {
            if (_started) return;
            _started = true;

            // Start shortly after app boot, then every 5 minutes.
            _timer = new Timer(_ =>
            {
                try
                {
                    if (Interlocked.Exchange(ref _isRunning, 1) == 1) return;
                    try { EnforcePaymentDeadlines(); } finally { Interlocked.Exchange(ref _isRunning, 0); }
                }
                catch
                {
                    // Swallow exceptions: scheduled job should never crash the app.
                    Interlocked.Exchange(ref _isRunning, 0);
                }
            }, null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(5));
        }

        // Returns number of orders that were cancelled during this enforcement run.
        public static int EnforcePaymentDeadlines()
        {
            var nowUtc = DateTime.UtcNow;
            var cancelled = 0;

            // Pending phase includes states where payment is expected but not completed yet.
            // Deadline starts after admin approval, so we intentionally do NOT include PendingApproval/Pending.
            var pendingStatuses = new[]
            {
                OrderStatus.PendingPayment,
                OrderStatus.PendingDownpayment,
                OrderStatus.Approved,
                OrderStatus.AwaitingPaymentVerification
            };
            var activeStatuses = new[]
            {
                OrderStatus.LayawayActive
            };

            var statuses = pendingStatuses.Concat(activeStatuses).ToArray();

            using (var db = new SolteiraBrandsContext())
            {
                // Send reminders before enforcing cancellations.
                try { SendPaymentDeadlineReminders(db, nowUtc); } catch { /* don't break cancellation */ }

                var overdueOrderIds = db.tbl_orders
                    .Where(o =>
                        !o.IsArchived
                        && o.PaymentDeadline.HasValue
                        && o.PaymentDeadline.Value <= nowUtc
                        && statuses.Contains(o.OrderStatus))
                    .Select(o => o.Id)
                    .ToList();

                foreach (var orderId in overdueOrderIds)
                {
                    if (CancelOrder(orderId, null)) cancelled++;
                }
            }

            return cancelled;
        }

        private static void SendPaymentDeadlineReminders(SolteiraBrandsContext db, DateTime nowUtc)
        {
            // Send reminders only once per exact payment_deadline value (see tbl_orders.payment_deadline_reminder_sent_for).
            var leadDaysFull = PaymentDeadlineSettings.ReminderLeadDaysFullPayment;
            var leadDaysLayaway = PaymentDeadlineSettings.ReminderLeadDaysLayaway;
            var leadDaysResubmit = PaymentDeadlineSettings.ReminderLeadDaysResubmit;

            // Remind close to the target "remaining time" with a small tolerance window (job runs every 5 minutes).
            var reminderTolerance = TimeSpan.FromMinutes(30);
            var maxLeadDays = new[] { leadDaysFull, leadDaysLayaway, leadDaysResubmit }.Max();
            var maxTargetCenter = nowUtc.AddDays(maxLeadDays);
            var maxTargetEnd = maxTargetCenter + reminderTolerance;
            var minLeadDays = Math.Min(Math.Min(leadDaysFull, leadDaysLayaway), leadDaysResubmit);
            var minTargetCenter = nowUtc.AddDays(minLeadDays);
            var minTargetStart = minTargetCenter - reminderTolerance;

            var pendingStatuses = new[]
            {
                OrderStatus.PendingPayment,
                OrderStatus.PendingDownpayment,
                OrderStatus.Approved,
                OrderStatus.AwaitingPaymentVerification
            };
            var activeStatuses = new[]
            {
                OrderStatus.LayawayActive
            };
            var statuses = pendingStatuses.Concat(activeStatuses).ToArray();

            var layawayRow = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
            var downpaymentPct = layawayRow != null ? layawayRow.DownpaymentPercentage : 30m;

            var myOrdersUrl = "https://localhost:44327/SolteiraAndBrands/SolteiraBrandsMyOrders";

            var candidates = db.tbl_orders
                .Include("User")
                .Include("Payments")
                .Where(o =>
                    !o.IsArchived
                    && o.PaymentDeadline.HasValue
                    && o.PaymentDeadline.Value >= minTargetStart
                    && o.PaymentDeadline.Value < maxTargetEnd
                    && statuses.Contains(o.OrderStatus)
                )
                .ToList();

            foreach (var order in candidates)
            {
                if (order.PaymentDeadlineReminderSentFor.HasValue && order.PaymentDeadlineReminderSentFor.Value == order.PaymentDeadline.Value)
                    continue;

                if (order.User == null || string.IsNullOrWhiteSpace(order.User.Email))
                    continue;

                var paymentsList = order.Payments ?? new List<tblPaymentsModel>();
                var latestRejected = paymentsList
                    .Where(p => string.Equals(p.PaymentStatus, PaymentStatus.Rejected, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();
                var hasNewerSubmittedAfterLatestRejected = latestRejected != null && paymentsList.Any(p =>
                    string.Equals(p.PaymentStatus, PaymentStatus.Submitted, StringComparison.OrdinalIgnoreCase)
                    && p.CreatedAt > latestRejected.CreatedAt);

                var isResubmitWindow = latestRejected != null
                    && latestRejected.AllowResubmit != 0
                    && !hasNewerSubmittedAfterLatestRejected;
                var isLayaway = string.Equals(order.PaymentType ?? "", "LAYAWAY", StringComparison.OrdinalIgnoreCase);
                var leadDays = isResubmitWindow ? leadDaysResubmit : (isLayaway ? leadDaysLayaway : leadDaysFull);

                var targetCenter = nowUtc.AddDays(leadDays);
                var targetStart = targetCenter - reminderTolerance;
                var targetEnd = targetCenter + reminderTolerance;
                if (order.PaymentDeadline.Value < targetStart || order.PaymentDeadline.Value >= targetEnd)
                    continue;

                var verifiedPayments = paymentsList.Where(p => string.Equals(p.PaymentStatus, PaymentStatus.Verified, StringComparison.OrdinalIgnoreCase)).ToList();
                var totalPaidVerified = verifiedPayments.Sum(p => p.Amount);
                var amountDue = order.TotalAmount - totalPaidVerified;
                if (amountDue < 0) amountDue = 0;

                if (amountDue <= 0) continue;

                if (isLayaway)
                {
                    var verifiedCount = verifiedPayments.Count;
                    var remainingBalanceRecalc = order.TotalAmount - totalPaidVerified;
                    if (remainingBalanceRecalc < 0) remainingBalanceRecalc = 0;

                    // New layaway flow no longer depends on a fixed months plan.
                    // Reminder amount is downpayment before first verification, then current remaining balance.
                    amountDue = verifiedCount == 0
                        ? Math.Round(order.TotalAmount * (downpaymentPct / 100m), 2)
                        : remainingBalanceRecalc;
                    if (amountDue <= 0) continue;
                }

                var deadlineDate = order.PaymentDeadline.HasValue
                    ? PaymentDeadlineTime.FormatDeadlineLocal(order.PaymentDeadline.Value, "MM/dd/yyyy")
                    : null;
                var nameForSubject = !string.IsNullOrWhiteSpace(order.User.FirstName)
                    ? order.User.FirstName.Trim()
                    : (order.User.Username ?? "Customer");
                var orderNumber = order.OrderNumber ?? order.Id.ToString();
                var subject = isLayaway
                    ? nameForSubject + ", your layaway installment is due soon for order #" + orderNumber
                    : nameForSubject + ", payment due soon for order #" + orderNumber;
                var body = isLayaway
                    ? EmailHelper.BuildPaymentDeadlineReminderLayawayEmailHtml(order.User.FirstName ?? "", orderNumber, amountDue, deadlineDate, myOrdersUrl)
                    : EmailHelper.BuildPaymentDeadlineReminderFullEmailHtml(order.User.FirstName ?? "", orderNumber, amountDue, deadlineDate, myOrdersUrl);

                var sent = false;
                try
                {
                    sent = EmailHelper.SendEmail(order.User.Email.Trim(), subject, body, isBodyHtml: true);
                }
                catch { sent = false; }

                if (sent)
                {
                    order.PaymentDeadlineReminderSentFor = order.PaymentDeadline.Value;
                    order.PaymentDeadlineReminderSentAt = nowUtc;
                }
            }

            // Persist reminder markers.
            db.SaveChanges();
        }

        // Centralized, idempotent cancellation routine.
        public static bool CancelOrder(int orderId, string reason)
        {
            if (orderId <= 0) return false;
            var nowUtc = DateTime.UtcNow;

            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders
                    .Include("OrderItems")
                    .FirstOrDefault(o => o.Id == orderId);

                if (order == null) return false;
                if (order.IsArchived) return false;

                // Only cancel orders that are in the expected deadline-managed phases.
                if (!IsPendingOrActive(order.OrderStatus)) return false;

                // Idempotency: once cancelled, do nothing on future calls.
                if (string.Equals(order.OrderStatus, OrderStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
                    return false;

                order.OrderStatus = OrderStatus.Cancelled;
                // Auto-cancellation does not require a stored cancellation reason.
                // UI will show "expired" via PaymentDeadlineRemainingText when needed.
                order.CancellationReason = null;
                order.RejectionReason = null;
                order.LastStatusUpdateAt = nowUtc;

                if (!order.InventoryReleased)
                {
                    if (order.OrderItems != null)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            if (!item.ProductId.HasValue) continue;
                            var product = db.tbl_products.Find(item.ProductId.Value);
                            if (product != null)
                            {
                                product.StockQuantity += item.Quantity;
                                product.UpdatedAt = nowUtc;
                            }
                        }
                    }

                    order.InventoryReleased = true;
                }

                db.SaveChanges();
                return true;
            }
        }

        private static bool IsPendingOrActive(string orderStatus)
        {
            if (string.IsNullOrWhiteSpace(orderStatus)) return false;
            var s = orderStatus.Trim();

            // Pending phase states (deadline is active only after admin approval)
            if (string.Equals(s, OrderStatus.PendingPayment, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, OrderStatus.PendingDownpayment, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, OrderStatus.Approved, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, OrderStatus.AwaitingPaymentVerification, StringComparison.OrdinalIgnoreCase)) return true;

            // Layaway active phase state
            if (string.Equals(s, OrderStatus.LayawayActive, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }
    }
}

