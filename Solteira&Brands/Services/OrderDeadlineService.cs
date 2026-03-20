using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using Solteira_Brands.Constants;
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

