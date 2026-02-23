using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Solteira_Brands.Constants;
using Solteira_Brands.Models;
using Solteira_Brands.Models.Context;
using Solteira_Brands.Security;

namespace Solteira_Brands.Controllers
{
    public class SolteiraAndBrandsController : Controller
    {
        // ----- Order lifecycle: three status layers (order status is NEVER manually set) -----
        // Order status: Pending | Approved | Rejected | Awaiting Payment Verification | Paid | In Delivery | Ready for Meetup | Completed | Cancelled (action-driven only)
        // Payment status: SUBMITTED | VERIFIED | REJECTED
        // Delivery status: PENDING | IN_TRANSIT | DELIVERED

        /// <summary>Hash password with SHA256; returns hex string. Use when registering or comparing password.</summary>
        private static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>Record an activity log for the current user (admin/manager/staff). Call after the main action SaveChanges.</summary>
        private void LogActivity(SolteiraBrandsContext db, string action, string module, int? entityId, string details = null)
        {
            var username = User?.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return;
            var user = db.tbl_users.Include("Role").FirstOrDefault(u => u.Username == username);
            if (user == null) return;
            var roleName = user.Role != null ? user.Role.Name : "";
            var log = new tblActivityLogModel
            {
                UserId = user.Id,
                Username = user.Username ?? username,
                RoleName = roleName,
                Action = action ?? "",
                Module = module ?? "",
                EntityId = entityId,
                Details = details != null && details.Length > 500 ? details.Substring(0, 500) : details,
                CreatedAt = DateTime.Now
            };
            db.tbl_activity_logs.Add(log);
            db.SaveChanges();
        }

        // ----- SoD: ADMIN-only (Archive, Logs, Users) -----
        [Authorize(Roles = "ADMIN")]
        public ActionResult AdminOrdersArchivePage() { return View(); }
        [Authorize(Roles = "ADMIN")]
        public ActionResult AdminLogsPage() { return View(); }
        [Authorize(Roles = "ADMIN")]
        public ActionResult AdminUsersPage() { return View(); }

        // ----- SoD: MANAGER & STAFF only (Orders, Payment, Delivery) -----
        [Authorize(Roles = "MANAGER,STAFF")]
        public ActionResult AdminOrdersPage() { return View(); }
        [Authorize(Roles = "MANAGER,STAFF")]
        public ActionResult AdminOrderDetailsPage() { return View(); }
        [Authorize(Roles = "MANAGER,STAFF")]
        public ActionResult AdminPaymentPage() { return View(); }
        [Authorize(Roles = "MANAGER,STAFF")]
        public ActionResult AdminDeliveryShipmentsPage() { return View(); }
        [Authorize(Roles = "MANAGER,STAFF")]
        public ActionResult AdminDeliveryMeetupPage() { return View(); }

        // ----- Shared: ADMIN, MANAGER, STAFF (Dashboard, Inventory, Reports) -----
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminHomePage() { return View(); }
        /// <summary>Redirect to shared login.</summary>
        public ActionResult AdminLoginPage() { return RedirectToAction("SolteiraBrandsLogin"); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminPasswordChangePage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminPasswordChangePage1() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminDashboardPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminInventoryPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminReportsPage() { return View(); }

        // ----- E-COMMERCE PAGES -----
        public ActionResult SolteiraBrandsAbout()
        {
            return View();
        }
        /// <summary>Redirect to unified Account Settings (all roles).</summary>
        [Authorize]
        public ActionResult SolteiraBrandsAccountSettings()
        {
            return RedirectToAction("Settings", "Account");
        }

        [BuyerAuthorize]
        public ActionResult SolteiraBrandsCart()
        {
            return View();
        }

        [BuyerAuthorize]
        public ActionResult SolteiraBrandsCheckout()
        {
            return View();
        }
        public ActionResult SolteiraBrandsFaq()
        {
            return View();
        }
        public ActionResult SolteiraBrandsHome()
        {
            return View();
        }
        public ActionResult SolteiraBrandsJewelries()
        {
            return View();
        }
        [BuyerAuthorize]
        public ActionResult SolteiraBrandsMyOrders()
        {
            return View();
        }

        /// <summary>Buyer payment page for an approved order. Query: orderId.</summary>
        [BuyerAuthorize]
        public ActionResult SolteiraBrandsPayment()
        {
            return View();
        }

        // ----- PRODUCTS: Admin inventory (CRUA) -----
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetInventoryProducts(bool archived = false)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var now = DateTime.Now;
                var list = (from p in db.tbl_products
                            join img in db.tbl_images on p.ImageId equals img.Id into productImages
                            from img in productImages.DefaultIfEmpty()
                            where p.IsArchived == archived
                            orderby p.CreatedAt descending
                            select new
                            {
                                id = p.Id,
                                name = p.Name,
                                category = p.Category,
                                description = p.Description,
                                price = p.Price,
                                material = p.Material,
                                stones = p.Stones,
                                weight = p.Weight,
                                length = p.Length,
                                stockQuantity = p.StockQuantity,
                                imageUrl = img != null ? (img.ImagePath + img.ImageFileName) : null,
                                isBestSeller = p.IsBestSeller,
                                createdAt = p.CreatedAt,
                                updatedAt = p.UpdatedAt
                            }).ToList()
                            .Select(x => new
                            {
                                x.id,
                                x.name,
                                x.category,
                                x.description,
                                x.price,
                                x.material,
                                x.stones,
                                x.weight,
                                x.length,
                                x.stockQuantity,
                                x.imageUrl,
                                x.isBestSeller,
                                createdAtString = x.createdAt.ToString("MM/dd/yy"),
                                updatedAtString = x.updatedAt == DateTime.MinValue ? "N/A" : x.updatedAt.ToString("MM/dd/yy")
                            });

                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetLowStockNotifications()
        {
            const int lowStockThreshold = 3;
            using (var db = new SolteiraBrandsContext())
            {
                var items = (from p in db.tbl_products
                             join img in db.tbl_images on p.ImageId equals img.Id into imgs
                             from img in imgs.DefaultIfEmpty()
                             where !p.IsArchived && p.StockQuantity < lowStockThreshold
                             orderby p.StockQuantity, p.Name
                             select new
                             {
                                 id = p.Id,
                                 name = p.Name,
                                 stockQuantity = p.StockQuantity,
                                 imageUrl = img != null ? (img.ImagePath + img.ImageFileName) : (string)null
                             }).ToList();
                return Json(new { success = true, count = items.Count, items = items }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult CreateProduct(ProductSaveRequest request)
        {
            if (request == null) return Json(new { success = false, message = "Invalid payload." });
            using (var db = new SolteiraBrandsContext())
            {
                var now = DateTime.Now;
                var product = new tblProductsModel
                {
                    Name = (request.Name ?? string.Empty).Trim(),
                    Category = (request.Category ?? string.Empty).Trim(),
                    Description = (request.Description ?? string.Empty).Trim(),
                    Price = request.Price,
                    Material = request.Material,
                    Stones = request.Stones,
                    Weight = request.Weight,
                    Length = request.Length,
                    StockQuantity = request.StockQuantity,
                    ImageId = request.ImageId,
                    IsBestSeller = request.IsBestSeller,
                    IsArchived = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.tbl_products.Add(product);
                db.SaveChanges();
                LogActivity(db, "Add Product", "Inventory", product.Id, product.Name);
                return Json(new { success = true, message = "Product created successfully.", id = product.Id });
            }
        }

        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdateProduct(ProductSaveRequest request)
        {
            if (request == null || !request.Id.HasValue)
                return Json(new { success = false, message = "Invalid payload." });

            using (var db = new SolteiraBrandsContext())
            {
                var product = db.tbl_products.FirstOrDefault(p => p.Id == request.Id.Value);
                if (product == null) return Json(new { success = false, message = "Product not found." });

                product.Name = (request.Name ?? string.Empty).Trim();
                product.Category = (request.Category ?? string.Empty).Trim();
                product.Description = (request.Description ?? string.Empty).Trim();
                product.Price = request.Price;
                product.Material = request.Material;
                product.Stones = request.Stones;
                product.Weight = request.Weight;
                product.Length = request.Length;
                product.StockQuantity = request.StockQuantity;
                if (request.ImageId.HasValue) product.ImageId = request.ImageId;
                product.IsBestSeller = request.IsBestSeller;
                product.UpdatedAt = DateTime.Now;

                db.SaveChanges();
                LogActivity(db, "Edit Product", "Inventory", product.Id, product.Name);
                return Json(new { success = true, message = "Product updated successfully." });
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public JsonResult ToggleProductArchive(int id)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var product = db.tbl_products.FirstOrDefault(p => p.Id == id);
                if (product == null) return Json(new { success = false, message = "Product not found." });

                product.IsArchived = !product.IsArchived;
                product.UpdatedAt = DateTime.Now;
                db.SaveChanges();
                LogActivity(db, product.IsArchived ? "Archive Product" : "Unarchive Product", "Inventory", product.Id, product.Name);

                return Json(new { success = true, message = "Product archive status updated.", isArchived = product.IsArchived });
            }
        }

        // ----- PRODUCTS: Storefront listing (READ for buyers) -----
        [HttpGet]
        public JsonResult GetStorefrontProducts()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var products = (from p in db.tbl_products
                                join img in db.tbl_images on p.ImageId equals img.Id into productImages
                                from img in productImages.DefaultIfEmpty()
                                where !p.IsArchived
                                orderby p.Category, p.Name
                                select new
                                {
                                    id = p.Id,
                                    name = p.Name,
                                    price = p.Price,
                                    category = p.Category,
                                    material = p.Material,
                                    stones = p.Stones,
                                    weight = p.Weight,
                                    length = p.Length,
                                    stockQuantity = p.StockQuantity,
                                    img = img != null ? (img.ImagePath + img.ImageFileName) : null,
                                    isBestSeller = p.IsBestSeller
                                }).ToList();

                return Json(new { success = true, data = products }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>Returns current stock for one product. Used for add-to-cart and quantity validation. Server-side source of truth.</summary>
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetProductStock(int productId)
        {
            if (productId <= 0)
                return Json(new { success = false, message = "Invalid product." }, JsonRequestBehavior.AllowGet);
            using (var db = new SolteiraBrandsContext())
            {
                var product = db.tbl_products.FirstOrDefault(p => p.Id == productId && !p.IsArchived);
                if (product == null)
                    return Json(new { success = false, message = "Product not found." }, JsonRequestBehavior.AllowGet);
                return Json(new { success = true, productId = product.Id, stockQuantity = product.StockQuantity }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>Returns current stock for multiple products. Used for cart page to validate and auto-adjust quantities. GET with ?productIds=1&productIds=2</summary>
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetProductStocks(int[] productIds)
        {
            if (productIds == null || productIds.Length == 0)
                return Json(new { success = true, stocks = new object[0] }, JsonRequestBehavior.AllowGet);
            var ids = productIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
                return Json(new { success = true, stocks = new object[0] });
            using (var db = new SolteiraBrandsContext())
            {
                var stocks = db.tbl_products
                    .Where(p => ids.Contains(p.Id) && !p.IsArchived)
                    .Select(p => new { productId = p.Id, stockQuantity = p.StockQuantity })
                    .ToList();
                return Json(new { success = true, stocks }, JsonRequestBehavior.AllowGet);
            }
        }

        // ----- ORDERS: Create (buyer checkout), List/Details/UpdateStatus (admin) -----
        [BuyerAuthorize]
        [HttpPost]
        public JsonResult CreateOrder()
        {
            CreateOrderRequest request = null;
            try
            {
                using (var reader = new StreamReader(Request.InputStream))
                {
                    var body = reader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(body))
                        request = new JavaScriptSerializer().Deserialize<CreateOrderRequest>(body);
                }
            }
            catch { }
            if (request == null || request.Items == null || request.Items.Count == 0)
                return Json(new { success = false, message = "No items in order." });
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Not authenticated." });

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (user == null) return Json(new { success = false, message = "User not found." });

                if (string.IsNullOrEmpty((request.CustomerName ?? "").Trim()) || string.IsNullOrEmpty((request.ContactNumber ?? "").Trim()) || string.IsNullOrEmpty((request.DeliveryAddress ?? "").Trim()))
                    return Json(new { success = false, message = "Customer name, contact number, and delivery address are required." });

                // Server-side stock validation: ensure every item has sufficient stock before creating order
                var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
                var productsByKey = db.tbl_products.Where(p => productIds.Contains(p.Id)).ToDictionary(p => p.Id);
                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0)
                        return Json(new { success = false, message = "Invalid quantity for " + (item.ProductName ?? "item") + "." });
                    if (!productsByKey.TryGetValue(item.ProductId, out var product))
                        return Json(new { success = false, message = "Product not found: " + (item.ProductName ?? item.ProductId.ToString()) + "." });
                    if (product.StockQuantity < item.Quantity)
                        return Json(new { success = false, message = "Insufficient stock for " + (product.Name ?? item.ProductName) + ". Available: " + product.StockQuantity + "." });
                }

                var now = DateTime.Now;
                var paymentMethod = (request.PaymentMethod ?? "Full Payment").Trim();
                var paymentType = paymentMethod.IndexOf("Layaway", StringComparison.OrdinalIgnoreCase) >= 0 ? "LAYAWAY"
                    : paymentMethod.IndexOf("meetup", StringComparison.OrdinalIgnoreCase) >= 0 ? "CASH_MEETUP"
                    : "FULL";
                var deliveryType = (paymentType == "CASH_MEETUP" || paymentMethod.IndexOf("Meetup", StringComparison.OrdinalIgnoreCase) >= 0) ? "MEETUP" : "COURIER";
                var order = new tblOrdersModel
                {
                    UserId = user.Id,
                    OrderNumber = "",
                    CustomerName = (request.CustomerName ?? "").Trim(),
                    ContactNumber = (request.ContactNumber ?? "").Trim(),
                    DeliveryAddress = (request.DeliveryAddress ?? "").Trim(),
                    PaymentMethod = paymentMethod,
                    PaymentType = paymentType,
                    TotalAmount = request.TotalAmount,
                    RemainingBalance = request.TotalAmount,
                    OrderStatus = "Pending",
                    DeliveryType = deliveryType,
                    CreatedAt = now,
                    LastStatusUpdateAt = now
                };
                db.tbl_orders.Add(order);
                db.SaveChanges();

                order.OrderNumber = "ORD-" + order.Id;
                db.SaveChanges();

                foreach (var item in request.Items)
                {
                    db.tbl_order_items.Add(new tblOrderItemsModel
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName ?? "",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        ImageUrl = item.ImageUrl
                    });
                    // Deduct stock immediately after order is confirmed (order and items saved)
                    var prod = productsByKey[item.ProductId];
                    prod.StockQuantity -= item.Quantity;
                    prod.UpdatedAt = now;
                }
                db.SaveChanges();

                return Json(new { success = true, orderId = order.Id, orderNumber = order.OrderNumber, message = "Order placed successfully." });
            }
        }

        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetOrders()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var orders = db.tbl_orders
                    .Include("OrderItems")
                    .Include("Payments")
                    .Include("Deliveries")
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList();
                var list = orders.Select(o =>
                {
                    var latestDelivery = o.Deliveries != null && o.Deliveries.Any()
                        ? o.Deliveries.OrderByDescending(d => d.CreatedAt).First()
                        : null;
                    return new
                    {
                        id = o.Id,
                        orderNumber = o.OrderNumber,
                        orderDateString = o.CreatedAt.ToString("MM/dd/yyyy"),
                        itemCount = o.OrderItems != null ? o.OrderItems.Count : 0,
                        deliveryAddress = o.DeliveryAddress,
                        totalAmount = o.TotalAmount,
                        orderStatus = o.OrderStatus,
                        customerName = o.CustomerName,
                        contactNumber = o.ContactNumber,
                        paymentMethod = o.PaymentMethod,
                        hasPayments = o.Payments != null && o.Payments.Any(),
                        deliveryId = latestDelivery != null ? (int?)latestDelivery.Id : null,
                        deliveryStatus = latestDelivery != null ? latestDelivery.DeliveryStatus : null,
                        deliveryTrackingNumber = latestDelivery != null ? latestDelivery.TrackingNumber : null
                    };
                });
                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetOrderDetails(int id)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders
                    .Include("OrderItems")
                    .Include("User")
                    .Include("Payments")
                    .Include("Payments.ProofImage")
                    .Include("Deliveries")
                    .FirstOrDefault(o => o.Id == id);
                if (order == null) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

                var latestDelivery = order.Deliveries != null && order.Deliveries.Any()
                    ? order.Deliveries.OrderByDescending(d => d.CreatedAt).First()
                    : null;
                var hasPayments = order.Payments != null && order.Payments.Any();

                var items = order.OrderItems.Select(i => new
                {
                    productName = i.ProductName,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    imageUrl = i.ImageUrl
                }).ToList();

                var payments = (order.Payments ?? new List<tblPaymentsModel>()).OrderByDescending(p => p.CreatedAt).Select(p => new
                {
                    id = p.Id,
                    amount = p.Amount,
                    paymentStatus = p.PaymentStatus,
                    rejectionReason = p.RejectionReason,
                    proofImageUrl = p.ProofImage != null ? (p.ProofImage.ImagePath + p.ProofImage.ImageFileName) : null,
                    createdAt = p.CreatedAt,
                    verifiedAt = p.VerifiedAt
                }).ToList();

                return Json(new
                {
                    success = true,
                    order = new
                    {
                        id = order.Id,
                        orderNumber = order.OrderNumber,
                        orderStatus = order.OrderStatus,
                        createdAt = order.CreatedAt,
                        createdAtString = order.CreatedAt.ToString("MMM dd yyyy · hh:mm tt"),
                        customerName = order.CustomerName,
                        email = order.User != null ? order.User.Email : "",
                        contactNumber = order.ContactNumber,
                        deliveryAddress = order.DeliveryAddress,
                        paymentMethod = order.PaymentMethod,
                        totalAmount = order.TotalAmount,
                        items = items,
                        payments = payments,
                        hasPayments = hasPayments,
                        deliveryId = latestDelivery != null ? (int?)latestDelivery.Id : null,
                        deliveryTrackingNumber = latestDelivery != null ? latestDelivery.TrackingNumber : null,
                        deliveryStatus = latestDelivery != null ? latestDelivery.DeliveryStatus : null
                    }
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [BuyerAuthorize]
        [HttpGet]
        public JsonResult GetMyOrders()
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username)) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (user == null) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

                var orders = db.tbl_orders
                    .Where(o => o.UserId == user.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Include(o => o.Deliveries)
                    .Include(o => o.OrderItems)
                    .Include("Payments")
                    .Include("Payments.ProofImage")
                    .ToList();

                var layawayRow = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                var monthsAllowed = layawayRow != null ? layawayRow.MonthsAllowed : 3;
                var downpaymentPct = layawayRow != null ? layawayRow.DownpaymentPercentage : 30m;
                var resubmitWindowDays = (layawayRow != null && layawayRow.ResubmitWindowDays.HasValue) ? layawayRow.ResubmitWindowDays.Value : 3;
                var gracePeriodDays = (layawayRow != null && layawayRow.GracePeriodDays.HasValue) ? layawayRow.GracePeriodDays.Value : 0;

                var list = orders.Select(o =>
                {
                    var paymentsList = o.Payments ?? new List<tblPaymentsModel>();
                    var latestPaymentWithProof = paymentsList
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefault(p => p.ProofImageId.HasValue && p.ProofImage != null);
                    var latestPaymentProofUrl = latestPaymentWithProof?.ProofImage != null
                        ? (latestPaymentWithProof.ProofImage.ImagePath + latestPaymentWithProof.ProofImage.ImageFileName)
                        : (string)null;
                    var latestRejectedPayment = paymentsList
                        .Where(p => string.Equals(p.PaymentStatus, "REJECTED", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefault();
                    var latestPaymentRejectionReason = latestRejectedPayment?.RejectionReason;
                    var verifiedCount = paymentsList.Count(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase));
                    var paymentHistory = paymentsList
                        .Where(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p.VerifiedAt ?? p.CreatedAt)
                        .Select(p => new { amount = p.Amount, date = (p.VerifiedAt ?? p.CreatedAt).ToString("MMM d, yyyy") })
                        .ToList();
                    var orderMonths = string.Equals(o.PaymentType ?? "", "LAYAWAY", StringComparison.OrdinalIgnoreCase) ? monthsAllowed : 0;
                    var monthlyDueDates = orderMonths > 0
                        ? Enumerable.Range(1, orderMonths).Select(m => o.CreatedAt.AddMonths(m).ToString("MMM d, yyyy")).ToList()
                        : new List<string>();
                    List<object> installmentSchedule = null;
                    decimal? monthlyInstallment = null;
                    int? installmentPlanMonths = null;
                    var isLayaway = string.Equals(o.PaymentType ?? "", "LAYAWAY", StringComparison.OrdinalIgnoreCase);
                    if (orderMonths > 0 && (string.Equals(o.OrderStatus, "Layaway Active", StringComparison.OrdinalIgnoreCase) || (string.Equals(o.OrderStatus, "Approved", StringComparison.OrdinalIgnoreCase) && isLayaway)))
                    {
                        var totalAmount = o.TotalAmount;
                        var downpaymentAmount = Math.Round(totalAmount * (downpaymentPct / 100m), 2);
                        var remainingAfterDownpayment = Math.Round(totalAmount - downpaymentAmount, 2);
                        var monthlyInst = monthsAllowed > 0 ? Math.Round(remainingAfterDownpayment / monthsAllowed, 2) : remainingAfterDownpayment;
                        var lastMonthAmount = monthsAllowed > 1 ? Math.Round(remainingAfterDownpayment - (monthlyInst * (monthsAllowed - 1)), 2) : monthlyInst;
                        monthlyInstallment = monthlyInst;
                        installmentPlanMonths = monthsAllowed;
                        var verifiedPayments = paymentsList
                            .Where(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(p => p.VerifiedAt ?? p.CreatedAt)
                            .ToList();
                        var baseDate = verifiedPayments.Any()
                            ? (verifiedPayments.First().VerifiedAt ?? verifiedPayments.First().CreatedAt)
                            : o.CreatedAt;
                        var today = DateTime.Today;
                        var allPaymentsOrdered = paymentsList.OrderBy(p => p.CreatedAt).ToList();
                        installmentSchedule = new List<object>();
                        var foundNextUnpaid = false;

                        Func<tblPaymentsModel, string> getProofUrl = p => p != null && p.ProofImage != null ? (p.ProofImage.ImagePath + p.ProofImage.ImageFileName) : null;
                        Func<string, DateTime?, string> resolveStatus = (payStatus, rejectedAt) =>
                        {
                            if (string.Equals(payStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase)) return "Paid";
                            if (string.Equals(payStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase)) return "Awaiting Verification";
                            if (string.Equals(payStatus, "REJECTED", StringComparison.OrdinalIgnoreCase))
                            {
                                if (rejectedAt.HasValue && today > rejectedAt.Value.Date.AddDays(resubmitWindowDays)) return "Expired";
                                return "Rejected";
                            }
                            return "Pending";
                        };
                        Func<DateTime, string> pendingOrOverdue = due => due.Date.AddDays(gracePeriodDays) < today ? "Overdue" : "Pending";

                        string downpaymentRowStatus;
                        string downpaymentRejectionReason = null;
                        string downpaymentProofUrl = null;
                        if (verifiedCount >= 1)
                        {
                            downpaymentRowStatus = "Paid";
                        }
                        else if (allPaymentsOrdered.Count > 0)
                        {
                            var p0 = allPaymentsOrdered[0];
                            downpaymentProofUrl = getProofUrl(p0);
                            downpaymentRejectionReason = p0.RejectionReason;
                            downpaymentRowStatus = resolveStatus(p0.PaymentStatus, p0.VerifiedAt);
                            if (downpaymentRowStatus == "Pending")
                                downpaymentRowStatus = pendingOrOverdue(baseDate);
                        }
                        else
                            downpaymentRowStatus = pendingOrOverdue(baseDate);
                        var downpaymentIsNextUnpaid = downpaymentRowStatus != "Paid" && !foundNextUnpaid;
                        if (downpaymentIsNextUnpaid) foundNextUnpaid = true;
                        installmentSchedule.Add(new
                        {
                            rowType = "Downpayment",
                            label = "Downpayment",
                            monthNumber = 0,
                            dueDate = baseDate.ToString("MMM d, yyyy"),
                            amount = downpaymentAmount,
                            status = downpaymentRowStatus,
                            isNextUnpaid = downpaymentIsNextUnpaid,
                            proofImageUrl = downpaymentProofUrl,
                            rejectionReason = downpaymentRejectionReason
                        });
                        for (var i = 1; i <= monthsAllowed; i++)
                        {
                            var dueDate = baseDate.AddMonths(i);
                            var amt = (i == monthsAllowed) ? lastMonthAmount : monthlyInst;
                            string rowStatus;
                            string rowRejectionReason = null;
                            string rowProofUrl = null;
                            if (verifiedCount >= i + 1)
                                rowStatus = "Paid";
                            else if (allPaymentsOrdered.Count > i)
                            {
                                var pi = allPaymentsOrdered[i];
                                rowProofUrl = getProofUrl(pi);
                                rowRejectionReason = pi.RejectionReason;
                                rowStatus = resolveStatus(pi.PaymentStatus, pi.VerifiedAt);
                                if (rowStatus == "Pending")
                                    rowStatus = pendingOrOverdue(dueDate);
                            }
                            else
                                rowStatus = pendingOrOverdue(dueDate);
                            var isNextUnpaid = !foundNextUnpaid && rowStatus != "Paid";
                            if (isNextUnpaid) foundNextUnpaid = true;
                            installmentSchedule.Add(new
                            {
                                rowType = "Month",
                                label = "Month " + i,
                                monthNumber = i,
                                dueDate = dueDate.ToString("MMM d, yyyy"),
                                amount = amt,
                                status = rowStatus,
                                isNextUnpaid = isNextUnpaid,
                                proofImageUrl = rowProofUrl,
                                rejectionReason = rowRejectionReason
                            });
                        }
                    }
                    var monthsLeft = verifiedCount >= 1 ? (monthsAllowed - (verifiedCount - 1)) : monthsAllowed;
                    var latestSubOrRej = paymentsList.OrderByDescending(p => p.CreatedAt).FirstOrDefault(p => string.Equals(p.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase) || string.Equals(p.PaymentStatus, "REJECTED", StringComparison.OrdinalIgnoreCase));
                    object latestPaymentForProofDto = null;
                    if (latestSubOrRej != null)
                    {
                        var proofUrl = latestSubOrRej.ProofImage != null ? (latestSubOrRej.ProofImage.ImagePath + latestSubOrRej.ProofImage.ImageFileName) : null;
                        latestPaymentForProofDto = new
                        {
                            proofImageUrl = proofUrl,
                            referenceNumber = latestSubOrRej.ReferenceNumber,
                            amount = latestSubOrRej.Amount,
                            createdAt = latestSubOrRej.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                            paymentStatus = latestSubOrRej.PaymentStatus,
                            rejectionReason = latestSubOrRej.RejectionReason
                        };
                    }
                    return new
                    {
                        orderId = o.Id,
                        orderNumber = o.OrderNumber,
                        orderDate = o.CreatedAt.ToString("o"),
                        total = o.TotalAmount,
                        remainingBalance = o.RemainingBalance,
                        downpaymentPaid = orderMonths > 0 ? (o.TotalAmount - o.RemainingBalance) : (decimal?)null,
                        downpaymentPercentage = orderMonths > 0 ? (double)downpaymentPct : (double?)null,
                        monthlyInstallment = monthlyInstallment,
                        installmentPlanMonths = installmentPlanMonths,
                        installmentAmount = orderMonths > 0 && o.RemainingBalance > 0 && monthsLeft > 0 ? Math.Round(o.RemainingBalance / monthsLeft, 2) : (decimal?)null,
                        paymentType = o.PaymentType ?? "FULL",
                        status = o.OrderStatus,
                        rejectionReason = o.RejectionReason,
                        latestPaymentRejectionReason = latestPaymentRejectionReason,
                        deliveryStatus = o.Deliveries != null && o.Deliveries.Any()
                            ? o.Deliveries.OrderByDescending(d => d.CreatedAt).First().DeliveryStatus
                            : "PENDING",
                        trackingNumber = o.Deliveries != null && o.Deliveries.Any()
                            ? o.Deliveries.OrderByDescending(d => d.CreatedAt).First().TrackingNumber
                            : null,
                        createdAt = o.CreatedAt,
                        latestPaymentProofUrl = latestPaymentProofUrl,
                        verifiedPaymentsCount = verifiedCount,
                        monthsAllowed = monthsAllowed,
                        paymentHistory = paymentHistory,
                        monthlyDueDates = monthlyDueDates,
                        installmentSchedule = installmentSchedule,
                        layawayPolicyNote = orderMonths > 0 ? "Failure to settle installment within deadline may result in cancellation of Layaway agreement." : (string)null,
                        hasSubmittedPayment = paymentsList.Any(p => string.Equals(p.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase)),
                        latestPaymentForProof = latestPaymentForProofDto,
                        items = o.OrderItems.Select(i => new { name = i.ProductName, qty = i.Quantity, price = i.UnitPrice, img = i.ImageUrl }).ToList()
                    };
                }).ToList();

                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Single order for buyer payment page. Buyer only; returns order with order_status (exact case). Pay Now only when order_status === "Approved" and no existing payment.</summary>
        [BuyerAuthorize]
        [HttpGet]
        public JsonResult GetOrderForPayment(int orderId)
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username)) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (user == null) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                var order = db.tbl_orders.Include("OrderItems").Include("Payments").Include("Payments.ProofImage").FirstOrDefault(o => o.Id == orderId && o.UserId == user.Id);
                if (order == null) return Json(new { success = false, message = "Order not found." }, JsonRequestBehavior.AllowGet);
                bool hasExistingPayment = db.tbl_payments.Any(p => p.OrderId == order.Id);
                var latestPaymentForProof = order.Payments != null
                    ? order.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault(p => string.Equals(p.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase) || string.Equals(p.PaymentStatus, "REJECTED", StringComparison.OrdinalIgnoreCase))
                    : null;
                object latestPaymentForProofDto = null;
                if (latestPaymentForProof != null)
                {
                    var proofUrl = latestPaymentForProof.ProofImage != null ? (latestPaymentForProof.ProofImage.ImagePath + latestPaymentForProof.ProofImage.ImageFileName) : null;
                    latestPaymentForProofDto = new
                    {
                        proofImageUrl = proofUrl,
                        referenceNumber = latestPaymentForProof.ReferenceNumber,
                        amount = latestPaymentForProof.Amount,
                        createdAt = latestPaymentForProof.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                        paymentStatus = latestPaymentForProof.PaymentStatus,
                        rejectionReason = latestPaymentForProof.RejectionReason
                    };
                }
                var verifiedPaymentsCount = order.Payments != null ? order.Payments.Count(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase)) : 0;
                var orderStatus = order.OrderStatus ?? "";
                var paymentType = order.PaymentType ?? "FULL";
                decimal amountDue;
                decimal downpaymentPct = 30m;
                var layawayRow = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                var monthsAllowed = layawayRow != null ? layawayRow.MonthsAllowed : 3;
                if (layawayRow != null) downpaymentPct = layawayRow.DownpaymentPercentage;
                int monthsLeft = 0;
                DateTime? dueDateThisMonth = null;
                if (string.Equals(orderStatus, "Layaway Active", StringComparison.OrdinalIgnoreCase))
                {
                    monthsLeft = monthsAllowed - (verifiedPaymentsCount - 1);
                    if (monthsLeft > 0)
                        amountDue = Math.Round(order.RemainingBalance / monthsLeft, 2);
                    else
                        amountDue = order.RemainingBalance;
                    var verifiedPayments = order.Payments != null
                        ? order.Payments.Where(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.VerifiedAt ?? p.CreatedAt).ToList()
                        : new List<tblPaymentsModel>();
                    if (verifiedPayments.Any())
                    {
                        var baseDate = verifiedPayments.First().VerifiedAt ?? verifiedPayments.First().CreatedAt;
                        dueDateThisMonth = baseDate.AddMonths(verifiedPaymentsCount);
                    }
                }
                else if (paymentType == "LAYAWAY")
                    amountDue = Math.Round(order.TotalAmount * (downpaymentPct / 100m), 2);
                else
                    amountDue = order.RemainingBalance;
                var monthlyInstallmentVal = paymentType == "LAYAWAY" && monthsAllowed > 0
                    ? Math.Round((order.TotalAmount - (order.TotalAmount * (downpaymentPct / 100m))) / monthsAllowed, 2)
                    : (decimal?)null;
                int? installmentsRemaining = null;
                int? installmentMonthNumber = null;
                if (paymentType == "LAYAWAY")
                {
                    if (string.Equals(orderStatus, "Layaway Active", StringComparison.OrdinalIgnoreCase))
                    {
                        installmentsRemaining = monthsLeft;
                        installmentMonthNumber = verifiedPaymentsCount;
                    }
                    else
                    {
                        installmentsRemaining = monthsAllowed;
                        installmentMonthNumber = 0;
                    }
                }
                var data = new
                {
                    order_id = order.Id,
                    orderId = order.Id,
                    order_status = orderStatus,
                    order_number = order.OrderNumber,
                    orderNumber = order.OrderNumber,
                    payment_method = order.PaymentMethod ?? "",
                    customerName = order.CustomerName,
                    contactNumber = order.ContactNumber,
                    deliveryAddress = order.DeliveryAddress,
                    totalAmount = order.TotalAmount,
                    remainingBalance = order.RemainingBalance,
                    paymentType = paymentType,
                    amountDue = amountDue,
                    monthsAllowed = paymentType == "LAYAWAY" ? monthsAllowed : (int?)null,
                    verifiedPaymentsCount = paymentType == "LAYAWAY" ? verifiedPaymentsCount : (int?)null,
                    downpaymentPercentage = paymentType == "LAYAWAY" ? (double)downpaymentPct : (double?)null,
                    dueDateThisMonth = dueDateThisMonth.HasValue ? dueDateThisMonth.Value.ToString("MMM d, yyyy") : (string)null,
                    monthlyInstallment = monthlyInstallmentVal,
                    installmentsRemaining = installmentsRemaining,
                    installmentMonthNumber = installmentMonthNumber,
                    hasExistingPayment = hasExistingPayment,
                    latestPaymentForProof = latestPaymentForProofDto,
                    hasSubmittedPayment = order.Payments != null && order.Payments.Any(p => string.Equals(p.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase)),
                    items = order.OrderItems.Select(i => new { name = i.ProductName, qty = i.Quantity, price = i.UnitPrice, img = i.ImageUrl }).ToList()
                };
                return Json(new { success = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Active payment settings for buyer (GCASH and BANK_TRANSFER with QR URL, account name/number).</summary>
        [AllowAnonymous]
        [HttpGet]
        public JsonResult GetPaymentSettings()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var settings = db.tbl_payment_settings
                    .Include("QrImage")
                    .Where(s => s.IsActive)
                    .ToList();
                var gcash = settings.FirstOrDefault(s => string.Equals(s.PaymentType, "GCASH", StringComparison.OrdinalIgnoreCase));
                var bank = settings.FirstOrDefault(s => string.Equals(s.PaymentType, "BANK_TRANSFER", StringComparison.OrdinalIgnoreCase));
                var result = new
                {
                    gcash = gcash == null ? null : new
                    {
                        accountName = gcash.AccountName,
                        accountNumber = gcash.AccountNumber,
                        qrImageUrl = gcash.QrImage != null ? (gcash.QrImage.ImagePath + gcash.QrImage.ImageFileName) : null
                    },
                    bankTransfer = bank == null ? null : new
                    {
                        accountName = bank.AccountName,
                        accountNumber = bank.AccountNumber,
                        qrImageUrl = bank.QrImage != null ? (bank.QrImage.ImagePath + bank.QrImage.ImageFileName) : null
                    }
                };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Active meetup setting (location name and address).</summary>
        [AllowAnonymous]
        [HttpGet]
        public JsonResult GetMeetupSettings()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var m = db.tbl_meetup_settings.OrderByDescending(x => x.Id).FirstOrDefault(x => x.IsActive);
                if (m == null)
                    return Json(new { locationName = "", address = "" }, JsonRequestBehavior.AllowGet);
                return Json(new { locationName = m.LocationName, address = m.Address }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Layaway settings (downpayment %, months). Default 30, 3 if none.</summary>
        [AllowAnonymous]
        [HttpGet]
        public JsonResult GetLayawaySettings()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var row = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                if (row == null)
                    return Json(new { downpaymentPercentage = 30, monthsAllowed = 3 }, JsonRequestBehavior.AllowGet);
                return Json(new { downpaymentPercentage = (double)row.DownpaymentPercentage, monthsAllowed = row.MonthsAllowed }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>POST: Upload payment proof image (JPG or PNG only, same as avatar/QR). Saves to ~/Content/images_uploaded/paymentproof/, inserts tbl_images, returns imageID. Max 5MB. No PDF.</summary>
        [BuyerAuthorize]
        [HttpPost]
        public ActionResult UploadPaymentProof(HttpPostedFileBase file)
        {
            if (file == null && Request.Files != null && Request.Files.Count > 0)
                file = Request.Files["file"] ?? Request.Files[0];
            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "No file uploaded." });
            var fileName = file.FileName ?? "";
            var ext = (Path.GetExtension(fileName) ?? "").ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedExts.Contains(ext))
                return Json(new { success = false, message = "Only JPG or PNG allowed. PDF is not accepted." });
            if (file.ContentLength > 5 * 1024 * 1024)
                return Json(new { success = false, message = "File size exceeds 5MB. Please use a smaller image." });
            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var uploadPath = Server.MapPath("~/Content/images_uploaded/paymentproof");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            file.SaveAs(Path.Combine(uploadPath, uniqueFileName));
            const string imagePath = "/Content/images_uploaded/paymentproof/";
            var now = DateTime.Now;
            using (var db = new SolteiraBrandsContext())
            {
                var newImage = new tblImagesModel { ImagePath = imagePath, ImageFileName = uniqueFileName, CreatedAt = now, UpdatedAt = now };
                db.tbl_images.Add(newImage);
                db.SaveChanges();
                return Json(new { success = true, imageID = newImage.Id, imageId = newImage.Id, imageFileName = uniqueFileName, imagePath = imagePath });
            }
        }

        /// <summary>POST: Submit payment (same pattern as avatar/QR: upload proof to images_uploaded/paymentproof first, then submit with proofImageId). Non-meetup: referenceNumber + proofImageId required. Meetup: meetupDate + meetupTime required.</summary>
        [BuyerAuthorize]
        [HttpPost]
        public JsonResult SubmitPayment(SubmitPaymentRequest request)
        {
            if (request == null)
                return Json(new { success = false, message = "Invalid request." });
            var orderId = request.OrderId;
            var amount = request.Amount;
            var proofImageId = request.ProofImageId;
            var referenceNumber = request.ReferenceNumber;
            var meetupDate = request.MeetupDate;
            var meetupTime = request.MeetupTime;

            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username)) return Json(new { success = false, message = "Not authenticated." });
            if (orderId <= 0 || amount <= 0)
                return Json(new { success = false, message = "Invalid order or amount." });
            bool isMeetup = !string.IsNullOrWhiteSpace(meetupDate) && !string.IsNullOrWhiteSpace(meetupTime);
            string refTrimmed = null;
            if (!isMeetup)
            {
                if (!proofImageId.HasValue || proofImageId.Value <= 0)
                    return Json(new { success = false, message = "Proof of payment is required. Please upload your payment proof first." });
                refTrimmed = (referenceNumber ?? "").Trim();
                if (string.IsNullOrEmpty(refTrimmed))
                    return Json(new { success = false, message = "Reference number is required." });
                if (refTrimmed.Length > 20)
                    return Json(new { success = false, message = "Reference number must be 20 characters or less." });
            }
            DateTime? meetupDt = null;
            if (isMeetup)
            {
                if (!DateTime.TryParse(meetupDate, out var d) || !DateTime.TryParse(meetupTime, out var t))
                    return Json(new { success = false, message = "Valid meetup date and time are required." });
                meetupDt = d.Date.Add(t.TimeOfDay);
            }
            try
            {
                using (var db = new SolteiraBrandsContext())
                {
                    var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                    if (user == null) return Json(new { success = false, message = "User not found." });
                    var order = db.tbl_orders.FirstOrDefault(o => o.Id == orderId && o.UserId == user.Id);
                    if (order == null) return Json(new { success = false, message = "Order not found." });
                    if (!string.Equals(order.OrderStatus, "Approved", StringComparison.OrdinalIgnoreCase) && !string.Equals(order.OrderStatus, "Layaway Active", StringComparison.OrdinalIgnoreCase))
                        return Json(new { success = false, message = "Order is not available for payment." });
                    var orderPaymentType = (order.PaymentType ?? "FULL").ToUpperInvariant();
                    if (orderPaymentType == "LAYAWAY" && isMeetup)
                        return Json(new { success = false, message = "Layaway orders cannot use Cash through Meetup. Please use GCash or Bank Transfer." });
                    var now = DateTime.Now;
                    var payment = new tblPaymentsModel
                    {
                        OrderId = order.Id,
                        Amount = amount,
                        PaymentStatus = "SUBMITTED",
                        ProofImageId = isMeetup ? null : proofImageId,
                        ReferenceNumber = isMeetup ? null : refTrimmed,
                        MeetupDatetime = meetupDt,
                        CreatedAt = now
                    };
                    db.tbl_payments.Add(payment);
                    if (orderPaymentType != "LAYAWAY")
                    {
                        order.OrderStatus = OrderStatus.AwaitingPaymentVerification;
                        order.LastStatusUpdateAt = now;
                    }
                    db.SaveChanges();
                    var msg = isMeetup ? "Meetup scheduled. We will verify and update your order status." : "Payment submitted. We will verify and update your order status.";
                    return Json(new { success = true, message = msg });
                }
            }
            catch (Exception ex)
            {
                var errMsg = ex.Message ?? "Unable to submit payment. Please try again.";
                return Json(new { success = false, message = errMsg });
            }
        }

        /// <summary>GET: Admin list of payments (for Payments page). Returns payments with order and proof details.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetPaymentsForAdmin()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var payments = db.tbl_payments
                    .Include("Order")
                    .Include("ProofImage")
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();
                var list = payments.Select(p => new
                {
                    paymentId = p.Id,
                    orderId = p.OrderId,
                    orderNumber = p.Order != null ? p.Order.OrderNumber : null,
                    customerName = p.Order != null ? p.Order.CustomerName : null,
                    paymentDate = p.CreatedAt.ToString("o"),
                    paymentDateString = p.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                    paymentOption = p.Order != null ? (p.Order.PaymentType ?? "FULL") : null,
                    paymentMethod = p.Order != null ? p.Order.PaymentMethod : null,
                    amount = p.Amount,
                    paymentStatus = p.PaymentStatus,
                    referenceNumber = p.ReferenceNumber,
                    proofImageUrl = p.ProofImage != null ? (p.ProofImage.ImagePath + p.ProofImage.ImageFileName) : null,
                    rejectionReason = p.RejectionReason,
                    createdAt = p.CreatedAt
                }).ToList();
                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Admin — full payment detail for View modal. Order summary, payment history, installment schedule (if Layaway), meetup info (if Meetup).</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetPaymentDetailForAdmin(int paymentId)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var payment = db.tbl_payments
                    .Include("Order")
                    .Include("Order.OrderItems")
                    .Include("Order.User")
                    .Include("Order.Payments")
                    .Include("Order.Payments.ProofImage")
                    .Include("ProofImage")
                    .FirstOrDefault(p => p.Id == paymentId);
                if (payment == null || payment.Order == null)
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);

                var order = payment.Order;
                var paymentType = (order.PaymentType ?? "FULL").ToUpperInvariant();
                var deliveryType = (order.DeliveryType ?? "COURIER").ToUpperInvariant();
                var layawayRow = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                var monthsAllowed = layawayRow != null ? layawayRow.MonthsAllowed : 3;
                var downpaymentPct = layawayRow != null ? layawayRow.DownpaymentPercentage : 30m;

                var items = order.OrderItems.Select(i => new
                {
                    productName = i.ProductName,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    imageUrl = i.ImageUrl
                }).ToList();

                var paymentHistory = (order.Payments ?? new List<tblPaymentsModel>())
                    .OrderBy(p => p.VerifiedAt ?? p.CreatedAt)
                    .Select(p => new
                    {
                        paymentId = p.Id,
                        amount = p.Amount,
                        paymentStatus = p.PaymentStatus,
                        referenceNumber = p.ReferenceNumber,
                        rejectionReason = p.RejectionReason,
                        proofImageUrl = p.ProofImage != null ? (p.ProofImage.ImagePath + p.ProofImage.ImageFileName) : null,
                        createdAt = p.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                        verifiedAt = p.VerifiedAt.HasValue ? p.VerifiedAt.Value.ToString("MMM dd, yyyy hh:mm tt") : (string)null
                    }).ToList();

                List<object> installmentSchedule = null;
                if (paymentType == "LAYAWAY")
                {
                    var totalAmount = order.TotalAmount;
                    var downpaymentAmount = Math.Round(totalAmount * (downpaymentPct / 100m), 2);
                    var remainingAfterDownpayment = Math.Round(totalAmount - downpaymentAmount, 2);
                    var monthlyInstallment = monthsAllowed > 0 ? Math.Round(remainingAfterDownpayment / monthsAllowed, 2) : remainingAfterDownpayment;
                    var lastMonthAmount = monthsAllowed > 1 ? Math.Round(remainingAfterDownpayment - (monthlyInstallment * (monthsAllowed - 1)), 2) : monthlyInstallment;
                    var verifiedPayments = (order.Payments ?? new List<tblPaymentsModel>())
                        .Where(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p.VerifiedAt ?? p.CreatedAt)
                        .ToList();
                    var baseDate = verifiedPayments.Any()
                        ? (verifiedPayments.First().VerifiedAt ?? verifiedPayments.First().CreatedAt)
                        : order.CreatedAt;
                    var today = DateTime.Today;

                    installmentSchedule = new List<object>();
                    var downPayment = verifiedPayments.Count >= 1 ? verifiedPayments[0] : null;
                    installmentSchedule.Add(new
                    {
                        installmentNumber = 0,
                        label = "Downpayment",
                        dueDate = baseDate.ToString("MMM d, yyyy"),
                        amount = downpaymentAmount,
                        status = downPayment != null ? "Paid" : "Pending",
                        proofImageUrl = downPayment?.ProofImage != null ? (downPayment.ProofImage.ImagePath + downPayment.ProofImage.ImageFileName) : null,
                        rejectionReason = downPayment?.RejectionReason
                    });
                    for (var i = 1; i <= monthsAllowed; i++)
                    {
                        var dueDate = baseDate.AddMonths(i);
                        var amount = (i == monthsAllowed) ? lastMonthAmount : monthlyInstallment;
                        string status;
                        string proofDisplay = null;
                        string rejectionReasonDisplay = null;
                        var paidPayment = verifiedPayments.Count >= i + 1 ? verifiedPayments[i] : null;
                        if (paidPayment != null)
                        {
                            status = "Paid";
                            proofDisplay = paidPayment.ProofImage != null ? (paidPayment.ProofImage.ImagePath + paidPayment.ProofImage.ImageFileName) : null;
                            rejectionReasonDisplay = paidPayment.RejectionReason;
                        }
                        else if (dueDate.Date < today)
                        {
                            status = "Overdue";
                            proofDisplay = null;
                        }
                        else
                        {
                            status = "Pending";
                            proofDisplay = null;
                        }
                        installmentSchedule.Add(new
                        {
                            installmentNumber = i,
                            label = "Month " + i,
                            dueDate = dueDate.ToString("MMM d, yyyy"),
                            amount,
                            status,
                            proofImageUrl = proofDisplay,
                            rejectionReason = rejectionReasonDisplay
                        });
                    }
                }

                object meetupSchedule = null;
                if (deliveryType == "MEETUP")
                {
                    var meetupRow = db.tbl_meetup_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                    meetupSchedule = meetupRow != null ? new { locationName = meetupRow.LocationName ?? "", address = meetupRow.Address ?? "" } : null;
                }

                var paymentOverview = new
                {
                    paymentId = payment.Id,
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    amount = payment.Amount,
                    paymentType = paymentType,
                    paymentOption = order.PaymentType ?? "FULL",
                    referenceNumber = payment.ReferenceNumber,
                    paymentStatus = payment.PaymentStatus,
                    rejectionReason = payment.RejectionReason,
                    proofImageUrl = payment.ProofImage != null ? (payment.ProofImage.ImagePath + payment.ProofImage.ImageFileName) : null,
                    createdAtString = payment.CreatedAt.ToString("MMM dd yyyy · hh:mm tt")
                };

                var orderSummary = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    orderStatus = order.OrderStatus,
                    createdAtString = order.CreatedAt.ToString("MMM dd yyyy · hh:mm tt"),
                    customerName = order.CustomerName,
                    email = order.User != null ? order.User.Email : "",
                    contactNumber = order.ContactNumber,
                    deliveryAddress = order.DeliveryAddress,
                    paymentMethod = order.PaymentMethod,
                    totalAmount = order.TotalAmount,
                    items,
                    deliveryType
                };

                return Json(new
                {
                    success = true,
                    paymentOverview,
                    orderSummary,
                    paymentHistory,
                    installmentSchedule,
                    meetupSchedule,
                    selectedPayment = paymentOverview
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Admin — current payment settings (active GCASH and BANK_TRANSFER) for Manage Payment Settings modal.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetPaymentSettingsForAdmin()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var settings = db.tbl_payment_settings.Include("QrImage").Where(s => s.IsActive).ToList();
                var gcash = settings.FirstOrDefault(s => string.Equals(s.PaymentType, "GCASH", StringComparison.OrdinalIgnoreCase));
                var bank = settings.FirstOrDefault(s => string.Equals(s.PaymentType, "BANK_TRANSFER", StringComparison.OrdinalIgnoreCase));
                object toObj(tblPaymentSettingsModel s)
                {
                    if (s == null) return null;
                    return new
                    {
                        id = s.Id,
                        paymentType = s.PaymentType,
                        accountName = s.AccountName ?? "",
                        accountNumber = s.AccountNumber ?? "",
                        qrImageId = s.QrImageId,
                        qrImageUrl = s.QrImage != null ? (s.QrImage.ImagePath + s.QrImage.ImageFileName) : null
                    };
                }
                return Json(new { success = true, gcash = toObj(gcash), bankTransfer = toObj(bank) }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>POST: Admin — upload QR image for payment settings. Saves to ~/Content/images_uploaded/qr/, returns imageID.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public ActionResult UploadQrImage(HttpPostedFileBase file)
        {
            if (file == null && Request.Files != null && Request.Files.Count > 0)
                file = Request.Files["file"] ?? Request.Files[0];
            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "No file uploaded." });
            var fileName = file.FileName ?? "";
            var ext = (Path.GetExtension(fileName) ?? "").ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowed.Contains(ext))
                return Json(new { success = false, message = "Only JPG, JPEG, or PNG allowed for QR." });
            if (file.ContentLength > 5 * 1024 * 1024)
                return Json(new { success = false, message = "File size exceeds 5MB." });
            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var uploadPath = Server.MapPath("~/Content/images_uploaded/qr");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            file.SaveAs(Path.Combine(uploadPath, uniqueFileName));
            const string imagePath = "/Content/images_uploaded/qr/";
            var now = DateTime.Now;
            using (var db = new SolteiraBrandsContext())
            {
                var img = new tblImagesModel { ImagePath = imagePath, ImageFileName = uniqueFileName, CreatedAt = now, UpdatedAt = now };
                db.tbl_images.Add(img);
                db.SaveChanges();
                return Json(new { success = true, imageID = img.Id, imagePath = imagePath + uniqueFileName, imageFileName = uniqueFileName });
            }
        }

        /// <summary>POST: Admin — replace active config for GCASH or BANK_TRANSFER. Deactivates previous, inserts new, logs.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdatePaymentSetting(string paymentType, string accountName, string accountNumber, int? qrImageId)
        {
            var typeTrim = (paymentType ?? "").Trim().ToUpperInvariant();
            if (typeTrim != "GCASH" && typeTrim != "BANK_TRANSFER")
                return Json(new { success = false, message = "Payment type must be GCASH or BANK_TRANSFER." });
            using (var db = new SolteiraBrandsContext())
            {
                foreach (var s in db.tbl_payment_settings.Where(s => s.PaymentType == typeTrim && s.IsActive))
                    s.IsActive = false;
                var now = DateTime.Now;
                var newSetting = new tblPaymentSettingsModel
                {
                    PaymentType = typeTrim,
                    AccountName = (accountName ?? "").Trim(),
                    AccountNumber = (accountNumber ?? "").Trim(),
                    QrImageId = qrImageId > 0 ? qrImageId : null,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.tbl_payment_settings.Add(newSetting);
                db.SaveChanges();
                LogActivity(db, "Payment setting updated", "PaymentSettings", newSetting.Id, typeTrim);
                return Json(new { success = true, message = typeTrim + " settings saved." });
            }
        }

        /// <summary>GET: Admin — active meetup setting.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetMeetupSettingsForAdmin()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var m = db.tbl_meetup_settings.OrderByDescending(x => x.Id).FirstOrDefault(x => x.IsActive);
                if (m == null)
                    return Json(new { success = true, locationName = "", address = "" }, JsonRequestBehavior.AllowGet);
                return Json(new { success = true, locationName = m.LocationName, address = m.Address }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>POST: Admin — replace active meetup config. Deactivates previous, inserts new, logs.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdateMeetupSetting(string locationName, string address)
        {
            if (string.IsNullOrWhiteSpace(locationName))
                return Json(new { success = false, message = "Location name is required." });
            using (var db = new SolteiraBrandsContext())
            {
                foreach (var m in db.tbl_meetup_settings.Where(x => x.IsActive))
                    m.IsActive = false;
                var now = DateTime.Now;
                var newM = new tblMeetupSettingsModel
                {
                    LocationName = locationName.Trim(),
                    Address = (address ?? "").Trim(),
                    IsActive = true,
                    CreatedAt = now
                };
                db.tbl_meetup_settings.Add(newM);
                db.SaveChanges();
                LogActivity(db, "Meetup setting updated", "MeetupSettings", newM.Id, newM.LocationName);
                return Json(new { success = true, message = "Meetup location saved." });
            }
        }

        /// <summary>GET: Admin — layaway settings (single config).</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetLayawaySettingsForAdmin()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var row = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                if (row == null)
                    return Json(new { success = true, downpaymentPercentage = 30, monthsAllowed = 3 }, JsonRequestBehavior.AllowGet);
                return Json(new { success = true, downpaymentPercentage = (double)row.DownpaymentPercentage, monthsAllowed = row.MonthsAllowed }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>POST: Admin — update layaway config (single row: update or insert), log. ResubmitWindowDays and GracePeriodDays optional (defaults 3, 0).</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdateLayawaySettings(double downpaymentPercentage, int monthsAllowed, int resubmitWindowDays = 3, int gracePeriodDays = 0)
        {
            if (downpaymentPercentage <= 0 || downpaymentPercentage > 100)
                return Json(new { success = false, message = "Downpayment percentage must be between 1 and 100." });
            if (monthsAllowed < 1 || monthsAllowed > 24)
                return Json(new { success = false, message = "Months allowed must be 1–24." });
            if (resubmitWindowDays < 0 || resubmitWindowDays > 90)
                return Json(new { success = false, message = "Resubmit window must be 0–90 days." });
            if (gracePeriodDays < 0 || gracePeriodDays > 30)
                return Json(new { success = false, message = "Grace period must be 0–30 days." });
            using (var db = new SolteiraBrandsContext())
            {
                var row = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                var now = DateTime.Now;
                if (row != null)
                {
                    row.DownpaymentPercentage = (decimal)downpaymentPercentage;
                    row.MonthsAllowed = monthsAllowed;
                    row.ResubmitWindowDays = resubmitWindowDays;
                    row.GracePeriodDays = gracePeriodDays;
                }
                else
                {
                    db.tbl_layaway_settings.Add(new tblLayawaySettingsModel
                    {
                        DownpaymentPercentage = (decimal)downpaymentPercentage,
                        MonthsAllowed = monthsAllowed,
                        ResubmitWindowDays = resubmitWindowDays,
                        GracePeriodDays = gracePeriodDays,
                        CreatedAt = now
                    });
                }
                db.SaveChanges();
                LogActivity(db, "Layaway settings updated", "LayawaySettings", null, downpaymentPercentage + "% down, " + monthsAllowed + " months");
                return Json(new { success = true, message = "Layaway settings saved." });
            }
        }

        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetDashboardData(string range = "lastmonth", string startDate = null, string endDate = null)
        {
            var now = DateTime.Now;
            DateTime startOfRange;
            DateTime endOfRange;

            switch ((range ?? "").ToLowerInvariant())
            {
                case "today":
                    startOfRange = now.Date;
                    endOfRange = startOfRange.AddDays(1);
                    break;
                case "lastweek":
                    endOfRange = now.Date.AddDays(1);
                    startOfRange = endOfRange.AddDays(-7);
                    break;
                case "lastmonth":
                    endOfRange = now.Date.AddDays(1);
                    startOfRange = endOfRange.AddDays(-30);
                    break;
                case "custom":
                    if (!DateTime.TryParse(startDate, out startOfRange)) startOfRange = now.Date.AddMonths(-1);
                    if (!DateTime.TryParse(endDate, out endOfRange)) endOfRange = now.Date;
                    startOfRange = startOfRange.Date;
                    endOfRange = endOfRange.Date.AddDays(1);
                    if (startOfRange > endOfRange) { var t = startOfRange; startOfRange = endOfRange.AddDays(-1); endOfRange = t.AddDays(1); }
                    break;
                default:
                    endOfRange = now.Date.AddDays(1);
                    startOfRange = endOfRange.AddDays(-30);
                    break;
            }

            var daysInRange = (int)(endOfRange - startOfRange).TotalDays;
            if (daysInRange <= 0) daysInRange = 1;

            using (var db = new SolteiraBrandsContext())
            {
                var ordersInRange = db.tbl_orders.Where(o => o.CreatedAt >= startOfRange && o.CreatedAt < endOfRange).ToList();
                var orderIdsInRange = ordersInRange.Select(o => o.Id).ToList();

                var totalSales = ordersInRange.Sum(o => o.TotalAmount);
                var productSold = db.tbl_order_items.Where(oi => orderIdsInRange.Contains(oi.OrderId)).Sum(i => (int?)i.Quantity) ?? 0;
                var pendingOrders = ordersInRange.Count(o => o.OrderStatus == "Pending");
                var approvedOrders = ordersInRange.Count(o => o.OrderStatus == "Approved");
                // Count Completed (and legacy Shipped if any) for dashboard Orders card
                var shippedOrders = ordersInRange.Count(o => o.OrderStatus == "Completed" || o.OrderStatus == "Shipped");

                // Daily sales: one value per day in range
                var lineLabels = new List<string>();
                var lineData = new List<double>();
                for (var i = 0; i < daysInRange; i++)
                {
                    var dayStart = startOfRange.AddDays(i);
                    var dayEnd = dayStart.AddDays(1);
                    var dayTotal = ordersInRange.Where(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd).Sum(o => o.TotalAmount);
                    lineLabels.Add(dayStart.ToString("M/d"));
                    lineData.Add((double)dayTotal);
                }

                // Sales by category: only order items from orders in range
                var categories = new[] { "Ring", "Earring", "Necklace", "Bracelet" };
                var pieLabels = new List<string>();
                var pieDataList = new List<double>();
                foreach (var cat in categories)
                {
                    var total = (from oi in db.tbl_order_items
                                 join p in db.tbl_products on oi.ProductId equals p.Id
                                 where p.Category == cat && orderIdsInRange.Contains(oi.OrderId)
                                 select (decimal?)(oi.Quantity * oi.UnitPrice)).Sum() ?? 0;
                    pieLabels.Add(cat);
                    pieDataList.Add((double)total);
                }

                // Inventory stock: non-archived products with id, name, stockQuantity, imageUrl (status = Low Stock if quantity < 3, computed on frontend)
                var inventoryStock = (from p in db.tbl_products
                                     where !p.IsArchived
                                     join img in db.tbl_images on p.ImageId equals img.Id into imgs
                                     from img in imgs.DefaultIfEmpty()
                                     orderby p.Name
                                     select new
                                     {
                                         id = p.Id,
                                         name = p.Name,
                                         stockQuantity = p.StockQuantity,
                                         imageUrl = img != null ? (img.ImagePath + img.ImageFileName) : (string)null
                                     }).ToList();

                // Top selling products: group order_items by ProductId, sum Quantity, then get product details
                var topSold = db.tbl_order_items
                    .Where(oi => oi.ProductId != null)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new { ProductId = g.Key.Value, TotalSold = g.Sum(x => x.Quantity) })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(10)
                    .ToList();
                var topProductIds = topSold.Select(x => x.ProductId).ToList();
                var productDetails = (from p in db.tbl_products
                                      where topProductIds.Contains(p.Id)
                                      join img in db.tbl_images on p.ImageId equals img.Id into imgs
                                      from img in imgs.DefaultIfEmpty()
                                      select new { p.Id, p.Name, ImageUrl = img != null ? (img.ImagePath + img.ImageFileName) : (string)null }).ToDictionary(x => x.Id);
                var topSellingProducts = topSold
                    .Select(t => new
                    {
                        id = t.ProductId,
                        name = productDetails.ContainsKey(t.ProductId) ? productDetails[t.ProductId].Name : "Unknown",
                        imageUrl = productDetails.ContainsKey(t.ProductId) ? productDetails[t.ProductId].ImageUrl : (string)null,
                        totalSold = t.TotalSold
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    stats = new
                    {
                        totalSales,
                        productSold,
                        pendingOrders,
                        approvedOrders,
                        shippedOrders
                    },
                    dailySales = new { labels = lineLabels, data = lineData },
                    salesByCategory = new { labels = pieLabels, data = pieDataList },
                    inventoryStock,
                    topSellingProducts
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>Order status only via allowed transitions: Pending→Approved/Rejected; Approved→Rejected.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdateOrderStatus(int id, string status, string rejectionReason = null)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Json(new { success = false, message = "Status is required." });
            var statusTrim = status.Trim();
            if (!statusTrim.Equals("Approved", StringComparison.OrdinalIgnoreCase) && !statusTrim.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Only Approve or Reject is allowed." });

            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.Include("OrderItems").Include("Payments").FirstOrDefault(o => o.Id == id);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                var current = order.OrderStatus ?? "";

                if (string.Equals(statusTrim, "Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    if (!current.Equals("Pending", StringComparison.OrdinalIgnoreCase) && !current.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                        return Json(new { success = false, message = "Only Pending or Approved orders can be rejected." });
                    if (current.Equals("Approved", StringComparison.OrdinalIgnoreCase) && order.Payments != null && order.Payments.Any())
                        return Json(new { success = false, message = "Cannot reject: order already has payment(s). Reject only when no payment exists." });
                    if (string.IsNullOrWhiteSpace(rejectionReason))
                        return Json(new { success = false, message = "Rejection reason is required when rejecting an order." });
                    order.OrderStatus = "Rejected";
                    order.RejectionReason = rejectionReason.Trim();
                    order.LastStatusUpdateAt = DateTime.Now;
                    foreach (var item in order.OrderItems)
                    {
                        if (!item.ProductId.HasValue) continue;
                        var product = db.tbl_products.Find(item.ProductId.Value);
                        if (product != null) { product.StockQuantity += item.Quantity; product.UpdatedAt = DateTime.Now; }
                    }
                    LogActivity(db, "Order Rejected", "Orders", order.Id, "Order #" + (order.OrderNumber ?? id.ToString()) + "; reason: " + (order.RejectionReason ?? ""));
                }
                else
                {
                    if (!current.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                        return Json(new { success = false, message = "Only Pending orders can be approved." });
                    order.OrderStatus = "Approved";
                    order.RejectionReason = null;
                    order.LastStatusUpdateAt = DateTime.Now;
                    LogActivity(db, "Order Approved", "Orders", order.Id, "Order #" + (order.OrderNumber ?? id.ToString()));
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Order status updated.", orderStatus = order.OrderStatus });
            }
        }

        /// <summary>POST: Admin verify or reject payment. Only when payment is SUBMITTED. VERIFIED: COURIER+FULL→Paid; MEETUP→Ready for Meetup; LAYAWAY→Layaway Active or Paid when balance 0. REJECTED→order_status=Approved, save reason. Transaction + activity log.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdatePaymentStatus(int paymentId, string status, string rejectionReason = null)
        {
            var statusTrim = (status ?? "").Trim();
            if (statusTrim != "VERIFIED" && statusTrim != "REJECTED")
                return Json(new { success = false, message = "Status must be VERIFIED or REJECTED." });
            if (statusTrim == "REJECTED" && string.IsNullOrWhiteSpace(rejectionReason))
                return Json(new { success = false, message = "Rejection reason is required when rejecting payment." });
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username)) return Json(new { success = false, message = "Not authenticated." });
            using (var db = new SolteiraBrandsContext())
            {
                var adminUser = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (adminUser == null) return Json(new { success = false, message = "User not found." });
                var payment = db.tbl_payments.Include("Order").Include("Order.Payments").FirstOrDefault(p => p.Id == paymentId);
                if (payment == null) return Json(new { success = false, message = "Payment not found." });
                if (payment.Order == null) return Json(new { success = false, message = "Order not found." });
                if (!string.Equals(payment.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only SUBMITTED payments can be verified or rejected." });
                var now = DateTime.Now;
                payment.PaymentStatus = statusTrim;
                payment.VerifiedByUserId = adminUser.Id;
                payment.VerifiedAt = now;
                if (statusTrim == "REJECTED")
                {
                    payment.RejectionReason = rejectionReason.Trim();
                    var orderPaymentType = (payment.Order.PaymentType ?? "FULL").ToUpperInvariant();
                    if (orderPaymentType != "LAYAWAY" || !string.Equals(payment.Order.OrderStatus, "Layaway Active", StringComparison.OrdinalIgnoreCase))
                    {
                        payment.Order.OrderStatus = "Approved";
                        payment.Order.LastStatusUpdateAt = now;
                    }
                    LogActivity(db, "Payment Rejected", "Payments", payment.Id, "Order #" + (payment.Order.OrderNumber ?? payment.OrderId.ToString()) + "; reason: " + (payment.RejectionReason ?? ""));
                    db.SaveChanges();
                    return Json(new { success = true, message = "Payment rejected.", orderStatus = payment.Order.OrderStatus });
                }
                var order = payment.Order;
                var deliveryType = (order.DeliveryType ?? "COURIER").ToUpperInvariant();
                var paymentType = (order.PaymentType ?? "FULL").ToUpperInvariant();
                using (var trans = db.Database.BeginTransaction())
                {
                    try
                    {
                        if (paymentType == "LAYAWAY")
                        {
                            var verifiedCount = order.Payments != null ? order.Payments.Count(p => p.Id != payment.Id && string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase)) : 0;
                            if (verifiedCount == 0)
                            {
                                order.OrderStatus = "Layaway Active";
                                order.RemainingBalance = order.TotalAmount - payment.Amount;
                                if (order.RemainingBalance < 0) order.RemainingBalance = 0;
                                LogActivity(db, "Payment Verified (Layaway downpayment)", "Payments", payment.Id, "Order #" + (order.OrderNumber ?? order.Id.ToString()) + "; remaining: " + order.RemainingBalance.ToString("N2"));
                            }
                            else
                            {
                                order.RemainingBalance = order.RemainingBalance - payment.Amount;
                                if (order.RemainingBalance < 0) order.RemainingBalance = 0;
                                if (order.RemainingBalance <= 0)
                                {
                                    order.OrderStatus = deliveryType == "MEETUP" ? "Ready for Meetup" : "Paid";
                                    LogActivity(db, "Payment Verified (Layaway final)", "Payments", payment.Id, "Order #" + (order.OrderNumber ?? order.Id.ToString()) + " fully paid.");
                                }
                                else
                                    LogActivity(db, "Payment Verified (Layaway installment)", "Payments", payment.Id, "Order #" + (order.OrderNumber ?? order.Id.ToString()) + "; remaining: " + order.RemainingBalance.ToString("N2"));
                            }
                        }
                        else
                        {
                            order.RemainingBalance = 0;
                            if (deliveryType == "MEETUP")
                            {
                                order.OrderStatus = "Ready for Meetup";
                                LogActivity(db, "Payment Verified", "Payments", payment.Id, "Order #" + (order.OrderNumber ?? order.Id.ToString()) + " → Ready for Meetup.");
                            }
                            else
                            {
                                order.OrderStatus = "Paid";
                                LogActivity(db, "Payment Verified", "Payments", payment.Id, "Order #" + (order.OrderNumber ?? order.Id.ToString()));
                            }
                        }
                        order.LastStatusUpdateAt = now;
                        db.SaveChanges();
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
                return Json(new { success = true, message = "Payment verified.", orderStatus = order.OrderStatus });
            }
        }

        /// <summary>POST: Revoke Approval. Approved → Pending only if no payment record exists.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult RevokeApproval(int orderId)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.Include("OrderItems").Include("Payments").FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                if (!order.OrderStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only Approved orders can be revoked." });
                var hasPayments = order.Payments != null && order.Payments.Any();
                if (hasPayments)
                    return Json(new { success = false, message = "Cannot revoke: payment has already been submitted for this order." });
                order.OrderStatus = "Pending";
                order.LastStatusUpdateAt = DateTime.Now;
                db.SaveChanges();
                LogActivity(db, "Revoke Approval", "Orders", order.Id, "Order #" + (order.OrderNumber ?? orderId.ToString()));
                return Json(new { success = true, message = "Approval revoked. Order is Pending again.", orderStatus = order.OrderStatus });
            }
        }

        /// <summary>POST: Create delivery. Order stays Paid; status set to In Delivery when delivery status becomes IN_TRANSIT.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult CreateDelivery(int orderId, string deliveryType = null, string trackingNumber = null, string courier = null, string dispatchDate = null, string eta = null)
        {
            var deliveryTypeVal = (deliveryType ?? "").Trim();
            if (string.IsNullOrEmpty(deliveryTypeVal)) deliveryTypeVal = "COURIER";
            var now = DateTime.Now;
            DateTime? dispatchDt = null;
            DateTime? etaDt = null;
            if (!string.IsNullOrWhiteSpace(dispatchDate) && DateTime.TryParse(dispatchDate, out var d1)) dispatchDt = d1;
            if (!string.IsNullOrWhiteSpace(eta) && DateTime.TryParse(eta, out var d2)) etaDt = d2;
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.Include("Deliveries").FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                var ordStatus = order.OrderStatus ?? "";
                if (!ordStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) && !ordStatus.Equals("Ready for Meetup", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only Paid or Ready for Meetup orders can have delivery created." });
                if (order.Deliveries != null && order.Deliveries.Any())
                    return Json(new { success = false, message = "Delivery already exists for this order." });
                var courierVal = string.IsNullOrWhiteSpace(courier) ? "LBC" : courier.Trim();
                var delivery = new tblDeliveriesModel
                {
                    OrderId = order.Id,
                    DeliveryType = deliveryTypeVal,
                    Courier = courierVal,
                    TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim(),
                    DispatchDate = dispatchDt,
                    Eta = etaDt,
                    DeliveryStatus = "PENDING",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.tbl_deliveries.Add(delivery);
                db.SaveChanges();
                LogActivity(db, "Delivery Created", "Deliveries", delivery.Id, "Order #" + (order.OrderNumber ?? orderId.ToString()) + "; " + delivery.DeliveryType);
                return Json(new { success = true, message = "Delivery created.", orderStatus = order.OrderStatus, deliveryId = delivery.Id });
            }
        }

        /// <summary>POST: Update delivery status. In Transit or Delivered. Delivered → order Completed.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdateDeliveryStatus(int deliveryId, string status)
        {
            var statusTrim = (status ?? "").Trim();
            if (statusTrim != "IN_TRANSIT" && statusTrim != "DELIVERED")
                return Json(new { success = false, message = "Status must be IN_TRANSIT or DELIVERED." });
            var now = DateTime.Now;
            using (var db = new SolteiraBrandsContext())
            {
                var delivery = db.tbl_deliveries.Include("Order").FirstOrDefault(d => d.Id == deliveryId);
                if (delivery == null || delivery.Order == null) return Json(new { success = false, message = "Delivery not found." });
                var orderStatus = delivery.Order.OrderStatus ?? "";
                if (statusTrim == "IN_TRANSIT" && !orderStatus.Equals("In Delivery", StringComparison.OrdinalIgnoreCase) && !orderStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Order must be Paid or In Delivery." });
                if (statusTrim == "DELIVERED" && !orderStatus.Equals("In Delivery", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Order must be In Delivery before marking Delivered." });
                delivery.DeliveryStatus = statusTrim;
                delivery.UpdatedAt = now;
                if (statusTrim == "IN_TRANSIT")
                {
                    delivery.Order.OrderStatus = "In Delivery";
                    delivery.Order.LastStatusUpdateAt = now;
                }
                else if (statusTrim == "DELIVERED")
                {
                    delivery.Order.OrderStatus = "Completed";
                    delivery.Order.LastStatusUpdateAt = now;
                    LogActivity(db, "Order Completed", "Orders", delivery.Order.Id, "Order #" + (delivery.Order.OrderNumber ?? delivery.OrderId.ToString()));
                }
                db.SaveChanges();
                LogActivity(db, "Delivery Status " + statusTrim, "Deliveries", delivery.Id, "Order #" + (delivery.Order.OrderNumber ?? delivery.OrderId.ToString()));
                return Json(new { success = true, message = "Delivery status updated.", orderStatus = delivery.Order.OrderStatus, deliveryStatus = delivery.DeliveryStatus });
            }
        }

        /// <summary>POST: Paid → Ready for Meetup (no delivery record). Only when order is Paid.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult MarkReadyForMeetup(int orderId)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                if (!order.OrderStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only Paid orders can be marked Ready for Meetup." });
                order.OrderStatus = "Ready for Meetup";
                order.LastStatusUpdateAt = DateTime.Now;
                db.SaveChanges();
                LogActivity(db, "Ready for Meetup", "Orders", order.Id, "Order #" + (order.OrderNumber ?? orderId.ToString()));
                return Json(new { success = true, message = "Order is Ready for Meetup.", orderStatus = order.OrderStatus });
            }
        }

        /// <summary>POST: Ready for Meetup → Completed.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult CompleteMeetup(int orderId)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                if (!order.OrderStatus.Equals("Ready for Meetup", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only Ready for Meetup orders can be completed." });
                order.OrderStatus = "Completed";
                order.LastStatusUpdateAt = DateTime.Now;
                db.SaveChanges();
                LogActivity(db, "Meetup Completed", "Orders", order.Id, "Order #" + (order.OrderNumber ?? orderId.ToString()));
                return Json(new { success = true, message = "Order completed.", orderStatus = order.OrderStatus });
            }
        }

        /// <summary>GET: Orders for Delivery Shipments page. Paid (courier) or Ready for Meetup (meetup), remaining_balance = 0 for Layaway, no existing delivery.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetOrdersForDeliveryShipments()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var orders = db.tbl_orders
                    .Where(o => !o.Deliveries.Any()
                        && (o.OrderStatus == "Paid" || o.OrderStatus == "Ready for Meetup")
                        && (o.RemainingBalance <= 0 || o.PaymentType == null || (o.PaymentType != null && o.PaymentType.ToUpper() != "LAYAWAY")))
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new { id = o.Id, orderNumber = o.OrderNumber, customerName = o.CustomerName, deliveryAddress = o.DeliveryAddress, totalAmount = o.TotalAmount, paymentType = o.PaymentType ?? "FULL", createdAt = o.CreatedAt })
                    .ToList();
                return Json(new { success = true, data = orders }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Active shipments (order_status = In Delivery). One delivery per order.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetDeliveryShipments()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var deliveries = db.tbl_deliveries
                    .Include("Order")
                    .Where(d => d.Order != null && (d.Order.OrderStatus == "In Delivery" || d.Order.OrderStatus == "Paid"))
                    .OrderByDescending(d => d.CreatedAt)
                    .ToList();
                var list = deliveries.Select(d => new
                {
                    id = d.Id,
                    orderId = d.OrderId,
                    orderNumber = d.Order.OrderNumber,
                    customerName = d.Order.CustomerName,
                    deliveryAddress = d.Order.DeliveryAddress,
                    deliveryType = d.DeliveryType,
                    courier = d.Courier,
                    trackingNumber = d.TrackingNumber,
                    deliveryStatus = d.DeliveryStatus,
                    dispatchDateFormatted = (d.DispatchDate ?? d.CreatedAt).ToString("MMM d, yyyy"),
                    etaFormatted = d.Eta.HasValue ? d.Eta.Value.ToString("MMM d, yyyy") : null,
                    createdAt = d.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")
                }).ToList();
                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Admin — full delivery detail for View modal. Delivery overview, shipment info, order summary, customer, status timeline.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetDeliveryDetailForAdmin(int deliveryId)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var delivery = db.tbl_deliveries
                    .Include("Order")
                    .Include("Order.OrderItems")
                    .Include("Order.User")
                    .FirstOrDefault(d => d.Id == deliveryId);
                if (delivery == null || delivery.Order == null)
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);

                var order = delivery.Order;
                var items = order.OrderItems.Select(i => new { productName = i.ProductName, quantity = i.Quantity, unitPrice = i.UnitPrice }).ToList();
                var statusTimeline = new List<object> { new { status = "PENDING", at = delivery.CreatedAt.ToString("MMM dd, yyyy hh:mm tt") } };
                if (!string.Equals(delivery.DeliveryStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                    statusTimeline.Add(new { status = delivery.DeliveryStatus, at = delivery.UpdatedAt.ToString("MMM dd, yyyy hh:mm tt") });

                var dispatchDt = delivery.DispatchDate ?? delivery.CreatedAt;
                var deliveryOverview = new
                {
                    id = delivery.Id,
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    deliveryStatus = delivery.DeliveryStatus,
                    courier = delivery.Courier,
                    trackingNumber = delivery.TrackingNumber,
                    dispatchDateFormatted = dispatchDt.ToString("MMM d, yyyy"),
                    dispatchDateIso = dispatchDt.ToString("yyyy-MM-dd"),
                    etaFormatted = delivery.Eta.HasValue ? delivery.Eta.Value.ToString("MMM d, yyyy") : null,
                    etaIso = delivery.Eta.HasValue ? delivery.Eta.Value.ToString("yyyy-MM-dd") : (string)null,
                    deliveryType = delivery.DeliveryType,
                    createdAtString = delivery.CreatedAt.ToString("MMM dd yyyy · hh:mm tt")
                };
                var orderSummary = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    orderStatus = order.OrderStatus,
                    customerName = order.CustomerName,
                    email = order.User != null ? order.User.Email : "",
                    contactNumber = order.ContactNumber,
                    deliveryAddress = order.DeliveryAddress,
                    totalAmount = order.TotalAmount,
                    items
                };
                return Json(new
                {
                    success = true,
                    deliveryOverview,
                    orderSummary,
                    statusTimeline
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>POST: Update delivery (shipment). Optional deliveryStatus: PENDING, IN_TRANSIT, DELIVERED. IN_TRANSIT → order In Delivery; DELIVERED → order Completed.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdateDelivery(int deliveryId, string courier = null, string trackingNumber = null, string dispatchDate = null, string eta = null, string deliveryStatus = null)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var delivery = db.tbl_deliveries.Include("Order").FirstOrDefault(d => d.Id == deliveryId);
                if (delivery == null || delivery.Order == null)
                    return Json(new { success = false, message = "Delivery not found." });
                var currentStatus = (delivery.DeliveryStatus ?? "").Trim();
                if (string.Equals(currentStatus, "DELIVERED", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Cannot edit a delivered shipment." });

                var now = DateTime.Now;
                var newStatus = (deliveryStatus ?? "").Trim();
                if (!string.IsNullOrEmpty(newStatus) && (newStatus.Equals("PENDING", StringComparison.OrdinalIgnoreCase) || newStatus.Equals("IN_TRANSIT", StringComparison.OrdinalIgnoreCase) || newStatus.Equals("DELIVERED", StringComparison.OrdinalIgnoreCase)))
                {
                    delivery.DeliveryStatus = newStatus;
                    if (newStatus.Equals("IN_TRANSIT", StringComparison.OrdinalIgnoreCase))
                    {
                        delivery.Order.OrderStatus = "In Delivery";
                        delivery.Order.LastStatusUpdateAt = now;
                    }
                    else if (newStatus.Equals("DELIVERED", StringComparison.OrdinalIgnoreCase))
                    {
                        delivery.Order.OrderStatus = "Completed";
                        delivery.Order.LastStatusUpdateAt = now;
                    }
                }
                var status = (delivery.DeliveryStatus ?? "").Trim();
                if (string.Equals(status, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    if (courier != null) delivery.Courier = courier.Trim();
                    if (trackingNumber != null) delivery.TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
                    DateTime d;
                    if (!string.IsNullOrWhiteSpace(dispatchDate) && DateTime.TryParse(dispatchDate, out d))
                        delivery.DispatchDate = d;
                    if (!string.IsNullOrWhiteSpace(eta) && DateTime.TryParse(eta, out d))
                        delivery.Eta = d;
                }
                else if (string.Equals(status, "IN_TRANSIT", StringComparison.OrdinalIgnoreCase))
                {
                    if (trackingNumber != null) delivery.TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
                }
                delivery.UpdatedAt = now;
                db.SaveChanges();
                LogActivity(db, "Delivery Updated", "Deliveries", delivery.Id, "Order #" + (delivery.Order.OrderNumber ?? delivery.OrderId.ToString()));
                return Json(new { success = true, message = "Delivery updated.", deliveryStatus = delivery.DeliveryStatus, orderStatus = delivery.Order.OrderStatus });
            }
        }

        /// <summary>GET: Orders for Delivery Meetup page. Only order_status = Ready for Meetup.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetOrdersForDeliveryMeetup()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var orders = db.tbl_orders
                    .Where(o => o.OrderStatus == "Ready for Meetup")
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new { id = o.Id, orderNumber = o.OrderNumber, customerName = o.CustomerName, deliveryAddress = o.DeliveryAddress, contactNumber = o.ContactNumber, totalAmount = o.TotalAmount, createdAt = o.CreatedAt })
                    .ToList();
                return Json(new { success = true, data = orders }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Admin — meetup order detail for View modal. Order summary, customer, meetup schedule (location), scheduled date/time from payment.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetMeetupOrderDetail(int orderId)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders
                    .Include("OrderItems")
                    .Include("User")
                    .Include("Payments")
                    .FirstOrDefault(o => o.Id == orderId);
                if (order == null || !string.Equals(order.OrderStatus, "Ready for Meetup", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                var meetupRow = db.tbl_meetup_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                var scheduledPayment = order.Payments != null ? order.Payments.FirstOrDefault(p => p.MeetupDatetime.HasValue) : null;
                var scheduledAt = scheduledPayment?.MeetupDatetime;
                var items = order.OrderItems.Select(i => new { productName = i.ProductName, quantity = i.Quantity, unitPrice = i.UnitPrice }).ToList();
                var orderSummary = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    orderStatus = order.OrderStatus,
                    customerName = order.CustomerName,
                    email = order.User != null ? order.User.Email : "",
                    contactNumber = order.ContactNumber,
                    deliveryAddress = order.DeliveryAddress,
                    totalAmount = order.TotalAmount,
                    items
                };
                var meetupSchedule = meetupRow != null ? new { locationName = meetupRow.LocationName ?? "", address = meetupRow.Address ?? "" } : null;
                var scheduledAtFormatted = scheduledAt.HasValue ? scheduledAt.Value.ToString("MMM dd, yyyy hh:mm tt") : null;
                return Json(new
                {
                    success = true,
                    orderSummary,
                    meetupSchedule,
                    scheduledAtFormatted
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>POST: Upload meetup receipt (proof of transaction). JPG/PNG, max 5MB. Saves to ~/Content/images_uploaded/proofoftransactions/.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public ActionResult UploadMeetupReceipt(HttpPostedFileBase file)
        {
            if (file == null && Request.Files != null && Request.Files.Count > 0)
                file = Request.Files["file"] ?? Request.Files[0];
            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "No file uploaded." });
            var fileName = file.FileName ?? "";
            var ext = (Path.GetExtension(fileName) ?? "").ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedExts.Contains(ext))
                return Json(new { success = false, message = "Only JPG or PNG allowed." });
            if (file.ContentLength > 5 * 1024 * 1024)
                return Json(new { success = false, message = "File size exceeds 5MB." });
            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var uploadPath = Server.MapPath("~/Content/images_uploaded/proofoftransactions");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            file.SaveAs(Path.Combine(uploadPath, uniqueFileName));
            const string imagePath = "/Content/images_uploaded/proofoftransactions/";
            var now = DateTime.Now;
            using (var db = new SolteiraBrandsContext())
            {
                var newImage = new tblImagesModel { ImagePath = imagePath, ImageFileName = uniqueFileName, CreatedAt = now, UpdatedAt = now };
                db.tbl_images.Add(newImage);
                db.SaveChanges();
                return Json(new { success = true, imageID = newImage.Id, imageId = newImage.Id });
            }
        }

        /// <summary>POST: Update meetup outcome. COMPLETED requires receiptImageId; NO_SHOW keeps order Ready for Meetup; CANCELLED sets order Cancelled.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdateMeetupOutcome(int orderId, string status, int? receiptImageId = null)
        {
            var statusTrim = (status ?? "").Trim().ToUpperInvariant();
            if (statusTrim != "COMPLETED" && statusTrim != "NO_SHOW" && statusTrim != "CANCELLED")
                return Json(new { success = false, message = "Status must be COMPLETED, NO_SHOW, or CANCELLED." });
            if (statusTrim == "COMPLETED" && (!receiptImageId.HasValue || receiptImageId.Value <= 0))
                return Json(new { success = false, message = "Receipt upload is required for Completed." });
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                if (!string.Equals(order.OrderStatus, "Ready for Meetup", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only Ready for Meetup orders can be updated." });
                var now = DateTime.Now;
                if (statusTrim == "COMPLETED")
                {
                    order.OrderStatus = "Completed";
                    order.LastStatusUpdateAt = now;
                    db.tbl_meetup_outcomes.Add(new tblMeetupOutcomeModel { OrderId = orderId, Status = "COMPLETED", ReceiptImageId = receiptImageId, CreatedAt = now });
                    LogActivity(db, "Meetup Completed", "MeetupOutcomes", orderId, "Order #" + (order.OrderNumber ?? orderId.ToString()) + "; receipt uploaded.");
                }
                else if (statusTrim == "NO_SHOW")
                {
                    db.tbl_meetup_outcomes.Add(new tblMeetupOutcomeModel { OrderId = orderId, Status = "NO_SHOW", CreatedAt = now });
                    LogActivity(db, "Meetup No Show", "MeetupOutcomes", orderId, "Order #" + (order.OrderNumber ?? orderId.ToString()));
                }
                else
                {
                    order.OrderStatus = "Cancelled";
                    order.LastStatusUpdateAt = now;
                    db.tbl_meetup_outcomes.Add(new tblMeetupOutcomeModel { OrderId = orderId, Status = "CANCELLED", CreatedAt = now });
                    LogActivity(db, "Meetup Cancelled", "MeetupOutcomes", orderId, "Order #" + (order.OrderNumber ?? orderId.ToString()));
                }
                db.SaveChanges();
                return Json(new { success = true, message = "Meetup outcome updated.", orderStatus = order.OrderStatus });
            }
        }

        /// <summary>GET: Cancel orders that are Approved or Awaiting Payment Verification and older than 24h with no verified payment. Restore stock. Call from scheduler or manually.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult CheckAndCancelOverdueOrders()
        {
            var cutoff = DateTime.Now.AddHours(-24);
            var cancelled = 0;
            using (var db = new SolteiraBrandsContext())
            {
                var orders = db.tbl_orders
                    .Include("OrderItems")
                    .Include("Payments")
                    .Where(o => (o.OrderStatus == "Approved" || o.OrderStatus == "Awaiting Payment Verification") && o.CreatedAt < cutoff)
                    .ToList();
                foreach (var order in orders)
                {
                    var hasVerified = order.Payments != null && order.Payments.Any(p => p.PaymentStatus == "VERIFIED");
                    if (hasVerified) continue;
                    order.OrderStatus = "Cancelled";
                    order.LastStatusUpdateAt = DateTime.Now;
                    foreach (var item in order.OrderItems)
                    {
                        if (!item.ProductId.HasValue) continue;
                        var product = db.tbl_products.Find(item.ProductId.Value);
                        if (product != null) { product.StockQuantity += item.Quantity; product.UpdatedAt = DateTime.Now; }
                    }
                    LogActivity(db, "Order Cancelled (deadline)", "Orders", order.Id, "Order #" + (order.OrderNumber ?? order.Id.ToString()) + "; payment deadline exceeded.");
                    cancelled++;
                }
                db.SaveChanges();
            }
            return Json(new { success = true, cancelled = cancelled }, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public JsonResult GetActivityLogs()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var list = db.tbl_activity_logs
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => new
                    {
                        id = l.Id,
                        username = l.Username,
                        roleName = l.RoleName,
                        action = l.Action,
                        module = l.Module,
                        entityId = l.EntityId,
                        details = l.Details,
                        createdAt = l.CreatedAt
                    })
                    .ToList()
                    .Select(l => new
                    {
                        l.id,
                        l.username,
                        l.roleName,
                        l.action,
                        l.module,
                        l.entityId,
                        l.details,
                        createdAtString = l.createdAt.ToString("yyyy-MM-dd hh:mm tt")
                    });
                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetSalesReport(string range = "all", string startDate = null, string endDate = null, string category = null)
        {
            var now = DateTime.Now;
            DateTime startOfRange;
            DateTime endOfRange;
            var rangeLower = (range ?? "all").ToLowerInvariant();

            if (rangeLower == "all")
            {
                // No date filter: use a very wide range to include all orders
                startOfRange = DateTime.MinValue;
                endOfRange = DateTime.MaxValue;
            }
            else
            {
                switch (rangeLower)
                {
                    case "today":
                        startOfRange = now.Date;
                        endOfRange = startOfRange.AddDays(1);
                        break;
                    case "lastweek":
                        endOfRange = now.Date.AddDays(1);
                        startOfRange = endOfRange.AddDays(-7);
                        break;
                    case "lastmonth":
                        endOfRange = now.Date.AddDays(1);
                        startOfRange = endOfRange.AddDays(-30);
                        break;
                    case "custom":
                        if (!DateTime.TryParse(startDate, out startOfRange)) startOfRange = now.Date.AddMonths(-1);
                        if (!DateTime.TryParse(endDate, out endOfRange)) endOfRange = now.Date;
                        startOfRange = startOfRange.Date;
                        endOfRange = endOfRange.Date.AddDays(1);
                        if (startOfRange > endOfRange) { var t = startOfRange; startOfRange = endOfRange.AddDays(-1); endOfRange = t.AddDays(1); }
                        break;
                    default:
                        endOfRange = now.Date.AddDays(1);
                        startOfRange = endOfRange.AddDays(-30);
                        break;
                }
            }

            using (var db = new SolteiraBrandsContext())
            {
                var orderIdsInRange = rangeLower == "all"
                    ? db.tbl_orders.Select(o => o.Id).ToList()
                    : db.tbl_orders
                        .Where(o => o.CreatedAt >= startOfRange && o.CreatedAt < endOfRange)
                        .Select(o => o.Id)
                        .ToList();

                var query = from oi in db.tbl_order_items
                            join o in db.tbl_orders on oi.OrderId equals o.Id
                            join p in db.tbl_products on oi.ProductId equals p.Id into prodJoin
                            from p in prodJoin.DefaultIfEmpty()
                            where orderIdsInRange.Contains(oi.OrderId)
                            select new { oi, o, p };

                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All Categories", StringComparison.OrdinalIgnoreCase))
                {
                    var cat = category.Trim();
                    query = query.Where(x => x.p != null && x.p.Category == cat);
                }

                var rows = query
                    .OrderBy(x => x.o.CreatedAt)
                    .ThenBy(x => x.oi.Id)
                    .Select(x => new
                    {
                        date = x.o.CreatedAt,
                        orderNumber = x.o.OrderNumber,
                        itemName = x.oi.ProductName ?? "",
                        category = x.p != null ? x.p.Category : "N/A",
                        qty = x.oi.Quantity,
                        unitPrice = x.oi.UnitPrice,
                        total = x.oi.Quantity * x.oi.UnitPrice
                    })
                    .ToList()
                    .Select(x => new
                    {
                        date = x.date.ToString("yyyy-MM-dd"),
                        orderNumber = x.orderNumber ?? "",
                        itemName = x.itemName,
                        category = x.category ?? "N/A",
                        qty = x.qty,
                        unitPrice = x.unitPrice,
                        total = x.total
                    })
                    .ToList();

                var grandTotal = rows.Any() ? rows.Sum(x => x.total) : 0m;

                return Json(new { success = true, rows = rows, grandTotal = grandTotal }, JsonRequestBehavior.AllowGet);
            }
        }

        // ----- IMAGE UPLOAD (tbl_images; files to ~/Content/images_uploaded/products/) -----
        [HttpPost]
        public JsonResult UploadProductImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "No file uploaded." });
            var fileName = file.FileName ?? "";
            var ext = Path.GetExtension(fileName) ?? "";
            if (!ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Only JPG, JPEG, or PNG images are allowed." });
            }
            if (file.ContentLength > 5 * 1024 * 1024)
                return Json(new { success = false, message = "File size exceeds 5MB limit." });

            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string uploadPath = Server.MapPath("~/Content/images_uploaded/products");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            file.SaveAs(Path.Combine(uploadPath, uniqueFileName));

            const string imagePath = "/Content/images_uploaded/products/";
            var now = DateTime.Now;
            using (var db = new SolteiraBrandsContext())
            {
                var newImage = new tblImagesModel
                {
                    ImagePath = imagePath,
                    ImageFileName = uniqueFileName,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.tbl_images.Add(newImage);
                db.SaveChanges();
                return Json(new { success = true, imageID = newImage.Id, imageFileName = uniqueFileName, imagePath = imagePath });
            }
        }

        [HttpGet]
        public JsonResult GetProductImage(int imageId)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var image = db.tbl_images.FirstOrDefault(img => img.Id == imageId);
                if (image == null) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                string imageUrl = image.ImagePath + image.ImageFileName;
                return Json(new { success = true, imageUrl = imageUrl }, JsonRequestBehavior.AllowGet);
            }
        }

        // ----- ACCOUNT (Login, Register, OTP, Forgot/New Password) -----
        public ActionResult SolteiraBrandsLogin()
        {
            return View("~/Views/SolteiraBrandsAccount/SolteiraBrandsLogin.cshtml");
        }
        public ActionResult SolteiraBrandsRegister()
        {
            return View("~/Views/SolteiraBrandsAccount/SolteiraBrandsRegister.cshtml");
        }
        public ActionResult SolteiraBrandsOTP()
        {
            return View("~/Views/SolteiraBrandsAccount/SolteiraBrandsOTP.cshtml");
        }
        public ActionResult SolteiraBrandsForgotPassword()
        {
            return View("~/Views/SolteiraBrandsAccount/SolteiraBrandsForgotPassword.cshtml");
        }
        public ActionResult SolteiraBrandsNewPassword()
        {
            return View("~/Views/SolteiraBrandsAccount/SolteiraBrandsNewPassword.cshtml");
        }

        [Authorize]
        public ActionResult SolteiraBrandsChangePassword()
        {
            return View("~/Views/SolteiraBrandsAccount/SolteiraBrandsChangePassword.cshtml");
        }
    }
}