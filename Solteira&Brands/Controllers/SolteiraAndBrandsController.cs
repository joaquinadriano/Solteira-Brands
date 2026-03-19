using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Solteira_Brands.Constants;
using Solteira_Brands.Helpers;
using Solteira_Brands.Models;
using Solteira_Brands.Models.Context;
using Solteira_Brands.Security;
using Solteira_Brands.Services;

namespace Solteira_Brands.Controllers
{
    public class SolteiraAndBrandsController : Controller
    {
        // ----- Order lifecycle: three status layers (order status is NEVER manually set) -----
        // Order status: Pending | Approved | Rejected | Awaiting Payment Verification | Awaiting Meetup | Paid | In Delivery | Ready for Meetup | Completed | Cancelled (action-driven only)
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

        /// <summary>If order is Layaway and the next unpaid installment is past due (including grace period), cancel the order and restore stock.</summary>
        private void AutoCancelLayawayIfMissedPayment(tblOrdersModel order, SolteiraBrandsContext db)
        {
            if (order == null || db == null) return;
            if (!string.Equals(order.PaymentType ?? "", "LAYAWAY", StringComparison.OrdinalIgnoreCase)) return;
            if (string.Equals(order.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)) return;

            var layawayRow = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
            var monthsAllowed = layawayRow != null ? layawayRow.MonthsAllowed : 3;
            var gracePeriodDays = (layawayRow != null && layawayRow.GracePeriodDays.HasValue) ? layawayRow.GracePeriodDays.Value : 0;

            var paymentsList = order.Payments ?? new List<tblPaymentsModel>();
            var verifiedCount = paymentsList.Count(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase));
            if (verifiedCount >= monthsAllowed + 1) return;

            var verifiedPayments = paymentsList
                .Where(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.VerifiedAt ?? p.CreatedAt)
                .ToList();
            var baseDate = verifiedPayments.Any()
                ? (verifiedPayments.First().VerifiedAt ?? verifiedPayments.First().CreatedAt)
                : order.CreatedAt;

            var today = DateTime.Today;
            for (var i = 1; i <= monthsAllowed; i++)
            {
                if (verifiedCount >= i + 1) continue;
                var dueDate = baseDate.AddMonths(i);
                var graceEnd = dueDate.Date.AddDays(gracePeriodDays);
                if (today <= graceEnd) continue;
                order.OrderStatus = "Cancelled";
                order.LastStatusUpdateAt = DateTime.Now;
                if (order.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (!item.ProductId.HasValue) continue;
                        var product = db.tbl_products.Find(item.ProductId.Value);
                        if (product != null) { product.StockQuantity += item.Quantity; product.UpdatedAt = DateTime.Now; }
                    }
                }
                LogActivity(db, "Order Cancelled (layaway overdue)", "Orders", order.Id, "Order #" + (order.OrderNumber ?? order.Id.ToString()) + "; installment overdue.");
                db.SaveChanges();
                return;
            }
        }

        // ----- Orders + Archive: single page for ADMIN, MANAGER, STAFF (Show Active / Show Archived) -----
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminOrdersArchivePage()
        {
            ViewBag.Title = "Orders";
            return View();
        }
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public JsonResult GetArchiveOrders()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var orders = db.tbl_orders
                    .Include("OrderItems")
                    .Where(o => o.IsArchived)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList();
                var list = orders.Select(o => new
                {
                    id = o.Id,
                    orderNumber = o.OrderNumber,
                    orderDateString = o.CreatedAt.ToString("MM/dd/yyyy"),
                    itemCount = o.OrderItems != null ? o.OrderItems.Count : 0,
                    deliveryAddress = o.DeliveryAddress,
                    totalAmount = o.TotalAmount,
                    orderStatus = o.OrderStatus
                }).ToList();
                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
        }
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public JsonResult ArchiveOrder(int orderId)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                if (order.IsArchived) return Json(new { success = false, message = "Order is already archived." });
                order.IsArchived = true;
                db.SaveChanges();
                LogActivity(db, "Order Archived", "Orders", order.Id, "Order #" + (order.OrderNumber ?? orderId.ToString()));
                return Json(new { success = true, message = "Order archived successfully." });
            }
        }
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public JsonResult RestoreOrder(int orderId)
        {
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                if (!order.IsArchived) return Json(new { success = false, message = "Order is not archived." });
                order.IsArchived = false;
                db.SaveChanges();
                LogActivity(db, "Order Restored", "Orders", order.Id, "Order #" + (order.OrderNumber ?? orderId.ToString()));
                return Json(new { success = true, message = "Order restored successfully." });
            }
        }
        [Authorize(Roles = "ADMIN")]
        public ActionResult AdminLogsPage() { return View(); }
        [Authorize(Roles = "ADMIN")]
        public ActionResult AdminUsersPage() { return View(); }

        // ----- Redirect to combined Orders/Archive page -----
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminOrdersPage() { return RedirectToAction("AdminOrdersArchivePage"); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminOrderDetailsPage() { return RedirectToAction("AdminOrdersArchivePage"); }
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
                var validStatuses = new[] { "Paid", "In Delivery", "Completed", "Ready for Meetup" };
                var validOrderIds = db.tbl_orders.Where(o => !o.IsArchived && o.OrderStatus != null && validStatuses.Contains(o.OrderStatus)).Select(o => o.Id).ToList();
                var soldByProductId = db.tbl_order_items
                    .Where(oi => oi.ProductId != null && validOrderIds.Contains(oi.OrderId))
                    .GroupBy(oi => oi.ProductId.Value)
                    .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity) })
                    .ToList()
                    .ToDictionary(x => x.ProductId, x => x.Sold);

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
                                initialStock = p.InitialStock,
                                imageUrl = img != null ? (img.ImagePath + img.ImageFileName) : null,
                                isBestSeller = p.IsBestSeller,
                                isFeatured = p.IsFeatured,
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
                                x.initialStock,
                                sold = soldByProductId.ContainsKey(x.id) ? soldByProductId[x.id] : 0,
                                x.imageUrl,
                                x.isBestSeller,
                                x.isFeatured,
                                createdAtString = x.createdAt.ToString("MM/dd/yyyy"),
                                updatedAtString = x.updatedAt == DateTime.MinValue ? "N/A" : x.updatedAt.ToString("MM/dd/yyyy")
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
                var nameTrim = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(nameTrim))
                    return Json(new { success = false, message = "Product name is required." });

                // Enforce unique product name (case-insensitive) among non-archived products.
                var exists = db.tbl_products.Any(p =>
                    !p.IsArchived &&
                    p.Name != null &&
                    p.Name.Trim().ToLower() == nameTrim.ToLower());
                if (exists)
                    return Json(new { success = false, message = "A product with this name already exists. Please use a different name." });

                var product = new tblProductsModel
                {
                    Name = nameTrim,
                    Category = (request.Category ?? string.Empty).Trim(),
                    Description = (request.Description ?? string.Empty).Trim(),
                    Price = request.Price,
                    Material = request.Material,
                    Stones = request.Stones,
                    Weight = request.Weight,
                    Length = request.Length,
                    StockQuantity = request.StockQuantity,
                    InitialStock = request.StockQuantity,
                    ImageId = request.ImageId,
                    IsBestSeller = request.IsBestSeller,
                    IsFeatured = request.IsFeatured,
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

                var nameTrim = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(nameTrim))
                    return Json(new { success = false, message = "Product name is required." });

                // Enforce unique product name (case-insensitive) when editing as well.
                var exists = db.tbl_products.Any(p =>
                    p.Id != product.Id &&
                    !p.IsArchived &&
                    p.Name != null &&
                    p.Name.Trim().ToLower() == nameTrim.ToLower());
                if (exists)
                    return Json(new { success = false, message = "A product with this name already exists. Please use a different name." });

                product.Name = nameTrim;
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
                product.IsFeatured = request.IsFeatured;
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

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public JsonResult ToggleFeatured(int productId)
        {
            if (productId <= 0) return Json(new { success = false, message = "Invalid product." });
            using (var db = new SolteiraBrandsContext())
            {
                var product = db.tbl_products.FirstOrDefault(p => p.Id == productId);
                if (product == null) return Json(new { success = false, message = "Product not found." });
                product.IsFeatured = !product.IsFeatured;
                product.UpdatedAt = DateTime.Now;
                db.SaveChanges();
                LogActivity(db, product.IsFeatured ? "Feature Product" : "Unfeature Product", "Inventory", product.Id, product.Name);
                return Json(new { success = true, message = "Featured status updated.", isFeatured = product.IsFeatured });
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
                                    description = p.Description,
                                    price = p.Price,
                                    category = p.Category,
                                    material = p.Material,
                                    stones = p.Stones,
                                    weight = p.Weight,
                                    length = p.Length,
                                    stockQuantity = p.StockQuantity,
                                    img = img != null ? (img.ImagePath + img.ImageFileName) : null,
                                    isBestSeller = p.IsBestSeller,
                                    isFeatured = p.IsFeatured
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

        // ----- CART: per-account (server-side) -----
        private tblCartsModel GetOrCreateActiveCart(SolteiraBrandsContext db, tblUsersModel user)
        {
            var now = DateTime.Now;

            var cart = db.tbl_carts.FirstOrDefault(c => c.UserId == user.Id && c.IsActive);
            if (cart != null) return cart;

            cart = new tblCartsModel
            {
                UserId = user.Id,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.tbl_carts.Add(cart);
            db.SaveChanges();
            return cart;
        }

        [BuyerAuthorize]
        [HttpGet]
        public JsonResult GetCartCount()
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Not authenticated.", count = 0 }, JsonRequestBehavior.AllowGet);

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (user == null) return Json(new { success = false, message = "User not found.", count = 0 }, JsonRequestBehavior.AllowGet);

                var cart = db.tbl_carts.FirstOrDefault(c => c.UserId == user.Id && c.IsActive);
                if (cart == null) return Json(new { success = true, count = 0 }, JsonRequestBehavior.AllowGet);

                var count = db.tbl_cart_items.Where(ci => ci.CartId == cart.Id).Sum(ci => (int?)ci.Quantity) ?? 0;
                return Json(new { success = true, count }, JsonRequestBehavior.AllowGet);
            }
        }

        [BuyerAuthorize]
        [HttpGet]
        public JsonResult GetMyCart()
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Not authenticated.", items = new object[0] }, JsonRequestBehavior.AllowGet);

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (user == null)
                    return Json(new { success = false, message = "User not found.", items = new object[0] }, JsonRequestBehavior.AllowGet);

                var cart = db.tbl_carts.FirstOrDefault(c => c.UserId == user.Id && c.IsActive);
                if (cart == null) return Json(new { success = true, items = new object[0] }, JsonRequestBehavior.AllowGet);

                var cartItems = db.tbl_cart_items.Where(ci => ci.CartId == cart.Id).ToList();
                if (cartItems.Count == 0) return Json(new { success = true, items = new object[0] }, JsonRequestBehavior.AllowGet);

                var productIds = cartItems.Select(ci => ci.ProductId).Distinct().ToList();
                var products = db.tbl_products.Where(p => productIds.Contains(p.Id)).ToList();
                var productsById = products.ToDictionary(p => p.Id, p => p);

                // Keep cart consistent with current stock + product archive status.
                var changed = false;
                for (int i = cartItems.Count - 1; i >= 0; i--)
                {
                    var ci = cartItems[i];
                    if (!productsById.TryGetValue(ci.ProductId, out var p) || p.IsArchived)
                    {
                        db.tbl_cart_items.Remove(ci);
                        changed = true;
                        continue;
                    }
                    if (p.StockQuantity <= 0)
                    {
                        db.tbl_cart_items.Remove(ci);
                        changed = true;
                        continue;
                    }
                    if (ci.Quantity > p.StockQuantity)
                    {
                        ci.Quantity = p.StockQuantity;
                        ci.UpdatedAt = DateTime.Now;
                        changed = true;
                    }
                }

                if (changed) db.SaveChanges();

                // Re-load for response.
                var responseItems = (from ci in db.tbl_cart_items
                                      join p in db.tbl_products on ci.ProductId equals p.Id
                                      join img in db.tbl_images on p.ImageId equals img.Id into productImages
                                      from img in productImages.DefaultIfEmpty()
                                      where ci.CartId == cart.Id && !p.IsArchived
                                      orderby p.Category, p.Name
                                      select new
                                      {
                                          id = p.Id,
                                          name = p.Name,
                                          price = p.Price,
                                          img = img != null ? (img.ImagePath + img.ImageFileName) : null,
                                          stones = p.Stones,
                                          material = p.Material,
                                          weight = p.Weight,
                                          length = p.Length,
                                          qty = ci.Quantity,
                                          maxStock = p.StockQuantity
                                      }).ToList();

                return Json(new { success = true, items = responseItems }, JsonRequestBehavior.AllowGet);
            }
        }

        [BuyerAuthorize]
        [HttpPost]
        public JsonResult AddToCart(int productId, int quantity)
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Not authenticated." });

            if (productId <= 0)
                return Json(new { success = false, message = "Invalid product." });

            var requestedQty = Math.Max(1, quantity);

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (user == null) return Json(new { success = false, message = "User not found." });

                var product = db.tbl_products.FirstOrDefault(p => p.Id == productId && !p.IsArchived);
                if (product == null) return Json(new { success = false, message = "Product not found." });

                var now = DateTime.Now;

                if (product.StockQuantity <= 0)
                    return Json(new { success = false, message = product.Name + " is currently sold out." });

                var cart = GetOrCreateActiveCart(db, user);

                var item = db.tbl_cart_items.FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == productId);
                var existingQty = item != null ? item.Quantity : 0;
                var availableToAdd = product.StockQuantity - existingQty;

                if (availableToAdd <= 0)
                    return Json(new { success = false, message = "Maximum reached for this item." });

                var actualAdded = Math.Min(requestedQty, availableToAdd);
                if (actualAdded <= 0)
                    return Json(new { success = false, message = "Maximum reached for this item." });

                if (item == null)
                {
                    item = new tblCartItemsModel
                    {
                        CartId = cart.Id,
                        ProductId = productId,
                        Quantity = actualAdded,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    db.tbl_cart_items.Add(item);
                }
                else
                {
                    item.Quantity += actualAdded;
                    item.UpdatedAt = now;
                }
                db.SaveChanges();

                var cartCount = db.tbl_cart_items.Where(ci => ci.CartId == cart.Id).Sum(ci => (int?)ci.Quantity) ?? 0;

                return Json(new
                {
                    success = true,
                    addedQuantity = actualAdded,
                    itemQuantity = item.Quantity,
                    maxStock = product.StockQuantity,
                    cartCount
                });
            }
        }

        [BuyerAuthorize]
        [HttpPost]
        public JsonResult SetCartItemQuantity(int productId, int quantity)
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Not authenticated." });

            if (productId <= 0)
                return Json(new { success = false, message = "Invalid product." });

            var desiredQty = quantity;

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (user == null) return Json(new { success = false, message = "User not found." });

                var product = db.tbl_products.FirstOrDefault(p => p.Id == productId && !p.IsArchived);
                if (product == null) return Json(new { success = false, message = "Product not found." });

                var cart = db.tbl_carts.FirstOrDefault(c => c.UserId == user.Id && c.IsActive);
                if (cart == null) return Json(new { success = false, message = "Cart not found." });

                var item = db.tbl_cart_items.FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == productId);
                if (item == null)
                    return Json(new { success = false, message = "Item not found in cart." });

                var now = DateTime.Now;
                if (desiredQty <= 0 || product.StockQuantity <= 0)
                {
                    db.tbl_cart_items.Remove(item);
                    db.SaveChanges();
                    return Json(new { success = true, removed = true });
                }

                var cappedQty = Math.Min(desiredQty, product.StockQuantity);
                if (cappedQty <= 0)
                {
                    db.tbl_cart_items.Remove(item);
                    db.SaveChanges();
                    return Json(new { success = true, removed = true });
                }

                item.Quantity = cappedQty;
                item.UpdatedAt = now;
                db.SaveChanges();

                return Json(new { success = true, quantity = item.Quantity, maxStock = product.StockQuantity });
            }
        }

        [BuyerAuthorize]
        [HttpPost]
        public JsonResult RemoveCartItem(int productId)
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Not authenticated." });

            if (productId <= 0)
                return Json(new { success = false, message = "Invalid product." });

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (user == null) return Json(new { success = false, message = "User not found." });

                var cart = db.tbl_carts.FirstOrDefault(c => c.UserId == user.Id && c.IsActive);
                if (cart == null) return Json(new { success = true, removed = true });

                var item = db.tbl_cart_items.FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == productId);
                if (item != null)
                {
                    db.tbl_cart_items.Remove(item);
                    db.SaveChanges();
                }

                return Json(new { success = true, removed = true });
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
                    OrderStatus = OrderStatus.PendingApproval,
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
                    .Where(o => !o.IsArchived)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList();
                var list = orders.Select(o =>
                {
                    var latestDelivery = o.Deliveries != null && o.Deliveries.Any()
                        ? o.Deliveries.OrderByDescending(d => d.CreatedAt).First()
                        : null;
                    string trackingNumberOnly = null;
                    string trackingLink = null;
                    if (latestDelivery != null)
                    {
                        // Prefer new split columns, but fallback to legacy "num||link" format in TrackingNumber.
                        trackingNumberOnly = latestDelivery.TrackingNumber;
                        trackingLink = latestDelivery.TrackingLink;
                        if (string.IsNullOrWhiteSpace(trackingLink) && !string.IsNullOrWhiteSpace(latestDelivery.TrackingNumber) && latestDelivery.TrackingNumber.Contains("||"))
                        {
                            var parts = latestDelivery.TrackingNumber.Split(new[] { "||" }, StringSplitOptions.None);
                            trackingNumberOnly = parts.Length > 0 ? parts[0] : null;
                            trackingLink = parts.Length > 1 ? parts[1] : null;
                        }
                    }

                    // Align admin Orders status with final cancellation logic:
                    // if the order is effectively cancelled via a final REJECTED payment
                    // (AllowResubmit = 0 and no newer SUBMITTED payment), show it as Cancelled
                    // instead of leaving it stuck on "Awaiting Payment Verification".
                    var status = o.OrderStatus;
                    var paymentsList = o.Payments ?? new List<tblPaymentsModel>();
                    if (!string.Equals(status, OrderStatus.Cancelled, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(status, OrderStatus.AwaitingPaymentVerification, StringComparison.OrdinalIgnoreCase))
                    {
                        var latestRejected = paymentsList
                            .Where(p => string.Equals(p.PaymentStatus, "REJECTED", StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(p => p.CreatedAt)
                            .FirstOrDefault();
                        if (latestRejected != null && latestRejected.AllowResubmit == 0)
                        {
                            var hasNewerSubmitted = paymentsList.Any(p =>
                                string.Equals(p.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase)
                                && p.CreatedAt > latestRejected.CreatedAt);
                            if (!hasNewerSubmitted)
                            {
                                status = OrderStatus.Cancelled;
                            }
                        }
                    }

                    return new
                    {
                        id = o.Id,
                        orderNumber = o.OrderNumber,
                        orderDateString = o.CreatedAt.ToString("MM/dd/yyyy"),
                        itemCount = o.OrderItems != null ? o.OrderItems.Count : 0,
                        deliveryAddress = o.DeliveryAddress,
                        totalAmount = o.TotalAmount,
                        orderStatus = status,
                        paymentType = o.PaymentType ?? "FULL",
                        customerName = o.CustomerName,
                        contactNumber = o.ContactNumber,
                        paymentMethod = o.PaymentMethod,
                        hasPayments = paymentsList.Any(),
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

                string trackingNumberOnly = null;
                string trackingLink = null;
                if (latestDelivery != null)
                {
                    trackingNumberOnly = latestDelivery.TrackingNumber;
                    trackingLink = latestDelivery.TrackingLink;
                    if (string.IsNullOrWhiteSpace(trackingLink) && !string.IsNullOrWhiteSpace(latestDelivery.TrackingNumber) && latestDelivery.TrackingNumber.Contains("||"))
                    {
                        var parts = latestDelivery.TrackingNumber.Split(new[] { "||" }, StringSplitOptions.None);
                        trackingNumberOnly = parts.Length > 0 ? parts[0] : null;
                        trackingLink = parts.Length > 1 ? parts[1] : null;
                    }
                }

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

                var meetupScheduledAt = order.MeetupDatetime ?? (order.Payments != null ? order.Payments.FirstOrDefault(p => p.MeetupDatetime.HasValue)?.MeetupDatetime : null);
                return Json(new
                {
                    success = true,
                    order = new
                    {
                        id = order.Id,
                        orderNumber = order.OrderNumber,
                        orderStatus = order.OrderStatus,
                        createdAt = order.CreatedAt,
                        createdAtString = order.CreatedAt.ToString("MM/dd/yyyy hh:mm tt"),
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
                        deliveryTrackingNumber = trackingNumberOnly,
                        deliveryTrackingLink = trackingLink,
                        deliveryStatus = latestDelivery != null ? latestDelivery.DeliveryStatus : null,
                        deliveryType = order.DeliveryType,
                        meetupScheduledAtFormatted = meetupScheduledAt.HasValue ? meetupScheduledAt.Value.ToString("MMM dd, yyyy hh:mm tt") : (string)null
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
                    .Include("Payments.ProofImage")
                    .ToList();

                var layawayRow = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                var monthsAllowed = layawayRow != null ? layawayRow.MonthsAllowed : 3;
                var downpaymentPct = layawayRow != null ? layawayRow.DownpaymentPercentage : 30m;
                var resubmitWindowDays = (layawayRow != null && layawayRow.ResubmitWindowDays.HasValue) ? layawayRow.ResubmitWindowDays.Value : 3;
                var gracePeriodDays = (layawayRow != null && layawayRow.GracePeriodDays.HasValue) ? layawayRow.GracePeriodDays.Value : 0;

                var orderIds = orders.Select(o => o.Id).ToList();
                    var latestMeetupByOrder = db.tbl_meetup_outcomes
                        .Include("ReceiptImage")
                        .Where(m => orderIds.Contains(m.OrderId))
                        .GroupBy(m => m.OrderId)
                        .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

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
                    // Once an order is fully cancelled, treat it as terminal on the buyer side:
                    // do not surface any "resubmit" messaging based on an old rejection reason.
                    var isFinalCancelled =
                        string.Equals(o.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)
                        // Safety net: if the latest rejected payment is marked as final (allow_resubmit = 0)
                        // and there is no newer SUBMITTED payment, also treat the order as effectively cancelled
                        // for buyer UI purposes, even if the order_status string was not updated correctly.
                        || (latestRejectedPayment != null
                            && latestRejectedPayment.AllowResubmit == 0
                            && !paymentsList.Any(p =>
                                string.Equals(p.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase)
                                && p.CreatedAt > latestRejectedPayment.CreatedAt));
                    if (isFinalCancelled)
                    {
                        o.OrderStatus = OrderStatus.Cancelled;
                        latestPaymentRejectionReason = null;
                    }
                    var verifiedCount = paymentsList.Count(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase));
                    var totalPaidVerified = paymentsList.Where(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount);
                    var remainingBalanceRecalc = o.TotalAmount - totalPaidVerified;
                    if (remainingBalanceRecalc < 0) remainingBalanceRecalc = 0;
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
                    // Build layaway installment schedule for all layaway orders except Cancelled,
                    // regardless of temporary order status (Approved, Awaiting Payment Verification, etc.).
                    if (orderMonths > 0 && isLayaway && !string.Equals(o.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
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
                        // Latest payment per slot: assign each payment to a slot (0=down, 1..M=months), then take latest per slot.
                        var paymentsByCreated = paymentsList.OrderBy(p => p.CreatedAt).ToList();
                        var slotAssignments = new Dictionary<int, tblPaymentsModel>();
                        int nextSlot = 0;
                        foreach (var p in paymentsByCreated)
                        {
                            bool isVerified = string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase);
                            int slot = nextSlot;
                            if (slot <= monthsAllowed)
                                slotAssignments[slot] = p;
                            if (isVerified)
                                nextSlot++;
                        }
                        Func<tblPaymentsModel, string> getProofUrl = p => p != null && p.ProofImage != null ? (p.ProofImage.ImagePath + p.ProofImage.ImageFileName) : null;
                        Func<tblPaymentsModel, DateTime, string> buyerStatusForPayment = (pay, due) =>
                        {
                            if (pay == null) return due.Date.AddDays(gracePeriodDays) < today ? "Overdue" : "Pending";
                            var s = pay.PaymentStatus ?? "";
                            if (string.Equals(s, "VERIFIED", StringComparison.OrdinalIgnoreCase)) return "Paid";
                            if (string.Equals(s, "SUBMITTED", StringComparison.OrdinalIgnoreCase)) return "Awaiting Verification";
                            if (string.Equals(s, "REJECTED", StringComparison.OrdinalIgnoreCase))
                            {
                                var hasNewerSubmitted = paymentsList.Any(x => x.CreatedAt > pay.CreatedAt && string.Equals(x.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase));
                                if (hasNewerSubmitted) return "Awaiting Verification";
                                if (pay.VerifiedAt.HasValue && today > pay.VerifiedAt.Value.Date.AddDays(resubmitWindowDays)) return "Expired";
                                return "Rejected – Please Resubmit";
                            }
                            return due.Date.AddDays(gracePeriodDays) < today ? "Overdue" : "Pending";
                        };
                        Func<DateTime, string> pendingOrOverdue = due => due.Date.AddDays(gracePeriodDays) < today ? "Overdue" : "Pending";

                        installmentSchedule = new List<object>();
                        var foundNextUnpaid = false;
                        var latest0 = slotAssignments.ContainsKey(0) ? slotAssignments[0] : null;
                        string downpaymentRowStatus = latest0 != null ? buyerStatusForPayment(latest0, baseDate) : pendingOrOverdue(baseDate);
                        if (downpaymentRowStatus == "Pending" || downpaymentRowStatus == "Overdue") downpaymentRowStatus = pendingOrOverdue(baseDate);
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
                            proofImageUrl = getProofUrl(latest0),
                            rejectionReason = latest0?.RejectionReason
                        });
                        for (var i = 1; i <= monthsAllowed; i++)
                        {
                            var dueDate = baseDate.AddMonths(i);
                            var amt = (i == monthsAllowed) ? lastMonthAmount : monthlyInst;
                            var latestI = slotAssignments.ContainsKey(i) ? slotAssignments[i] : null;
                            string rowStatus = latestI != null ? buyerStatusForPayment(latestI, dueDate) : pendingOrOverdue(dueDate);
                            if (rowStatus == "Pending" || rowStatus == "Overdue") rowStatus = pendingOrOverdue(dueDate);
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
                                proofImageUrl = getProofUrl(latestI),
                                rejectionReason = latestI?.RejectionReason
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
                            rejectionReason = latestSubOrRej.RejectionReason,
                            allowResubmit = latestSubOrRej.AllowResubmit != 0
                        };
                    }

                    // Extract tracking number and link (if any) from latest delivery for this order.
                    var latestDeliveryForOrder = o.Deliveries != null && o.Deliveries.Any()
                        ? o.Deliveries.OrderByDescending(d => d.CreatedAt).First()
                        : null;
                    string trackingNumberOnly = null;
                    string trackingLink = null;
                    if (latestDeliveryForOrder != null && !string.IsNullOrWhiteSpace(latestDeliveryForOrder.TrackingNumber))
                    {
                        var tnRaw = latestDeliveryForOrder.TrackingNumber;
                        var tnParts = tnRaw.Split(new[] { "||" }, StringSplitOptions.None);
                        trackingNumberOnly = tnParts.Length > 0 ? tnParts[0] : null;
                        trackingLink = tnParts.Length > 1 ? tnParts[1] : null;
                    }

                    return new
                    {
                        orderId = o.Id,
                        orderNumber = o.OrderNumber,
                        orderDate = o.CreatedAt.ToString("o"),
                        total = o.TotalAmount,
                        totalPaidVerified = totalPaidVerified,
                        remainingBalance = remainingBalanceRecalc,
                        downpaymentPaid = orderMonths > 0 ? (decimal?)totalPaidVerified : (decimal?)null,
                        downpaymentPercentage = orderMonths > 0 ? (double)downpaymentPct : (double?)null,
                        monthlyInstallment = monthlyInstallment,
                        installmentPlanMonths = installmentPlanMonths,
                        installmentAmount = orderMonths > 0 && remainingBalanceRecalc > 0 && monthsLeft > 0 ? Math.Round(remainingBalanceRecalc / monthsLeft, 2) : (decimal?)null,
                        paymentType = o.PaymentType ?? "FULL",
                        status = o.OrderStatus,
                        rejectionReason = o.RejectionReason,
                        latestPaymentRejectionReason = latestPaymentRejectionReason,
                        deliveryStatus = o.Deliveries != null && o.Deliveries.Any()
                            ? o.Deliveries.OrderByDescending(d => d.CreatedAt).First().DeliveryStatus
                            : "PENDING",
                        trackingNumber = trackingNumberOnly,
                        trackingLink = trackingLink,
                        courier = o.Deliveries != null && o.Deliveries.Any()
                            ? o.Deliveries.OrderByDescending(d => d.CreatedAt).First().Courier
                            : null,
                        dispatchDate = o.Deliveries != null && o.Deliveries.Any()
                            ? (o.Deliveries.OrderByDescending(d => d.CreatedAt).First().DispatchDate ?? o.Deliveries.OrderByDescending(d => d.CreatedAt).First().CreatedAt).ToString("MMM d, yyyy")
                            : null,
                        eta = o.Deliveries != null && o.Deliveries.Any() && o.Deliveries.OrderByDescending(d => d.CreatedAt).First().Eta.HasValue
                            ? o.Deliveries.OrderByDescending(d => d.CreatedAt).First().Eta.Value.ToString("MMM d, yyyy")
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
                        latestPaymentStatus = latestSubOrRej != null ? latestSubOrRej.PaymentStatus : null,
                        latestPaymentForProof = latestPaymentForProofDto,
                        items = o.OrderItems.Select(i => new { name = i.ProductName, qty = i.Quantity, price = i.UnitPrice, img = i.ImageUrl }).ToList(),
                        deliveryType = o.DeliveryType ?? "",
                        meetupOutcomeStatus = latestMeetupByOrder.ContainsKey(o.Id) ? latestMeetupByOrder[o.Id].Status : (string)null,
                        meetupReason = (string)null,
                        meetupRescheduledDate = (string)null,
                        meetupRescheduledDateFormatted = (string)null,
                        meetupScheduledAtFormatted = (o.MeetupDatetime ?? (o.Payments != null ? o.Payments.FirstOrDefault(p => p.MeetupDatetime.HasValue)?.MeetupDatetime : null))?.ToString("MMM dd, yyyy hh:mm tt"),
                        meetupOutcomeAtFormatted = latestMeetupByOrder.ContainsKey(o.Id) ? latestMeetupByOrder[o.Id].CreatedAt.ToString("MMM dd, yyyy hh:mm tt") : (string)null,
                        meetupReceiptImageUrl = latestMeetupByOrder.ContainsKey(o.Id) && latestMeetupByOrder[o.Id].ReceiptImage != null ? (latestMeetupByOrder[o.Id].ReceiptImage.ImagePath + latestMeetupByOrder[o.Id].ReceiptImage.ImageFileName) : (string)null,
                        currentDate = DateTime.Today.ToString("MMM d, yyyy"),
                        isArchived = o.IsArchived
                    };
                }).ToList();

                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>GET: Single order for buyer payment page. All financial values recalculated from VERIFIED payments only; response never uses stored RemainingBalance.</summary>
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
                if (order.IsArchived) return Json(new { success = false, message = "Archived orders cannot be modified." }, JsonRequestBehavior.AllowGet);
                var paymentsList = order.Payments ?? new List<tblPaymentsModel>();
                // 1) Totals: VERIFIED only. Do NOT use order.RemainingBalance in response.
                var verifiedPayments = paymentsList.Where(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.VerifiedAt ?? p.CreatedAt).ToList();
                var verifiedPaymentsCount = verifiedPayments.Count;
                decimal totalPaidVerified = verifiedPayments.Sum(p => p.Amount);
                decimal remainingBalanceRecalc = order.TotalAmount - totalPaidVerified;
                if (remainingBalanceRecalc < 0) remainingBalanceRecalc = 0;
                // 2) Progress: (totalPaidVerified / TotalAmount) * 100, 2 decimals. VERIFIED only.
                double progressPercent = order.TotalAmount > 0 ? Math.Round((double)(totalPaidVerified / order.TotalAmount) * 100, 2) : 0;

                bool hasExistingPayment = paymentsList.Any(p => !string.Equals(p.PaymentStatus, "REJECTED", StringComparison.OrdinalIgnoreCase));
                var orderStatus = order.OrderStatus ?? "";
                var paymentType = (order.PaymentType ?? "FULL").ToUpperInvariant();
                decimal downpaymentPct = 30m;
                var layawayRow = db.tbl_layaway_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                var monthsAllowed = layawayRow != null ? layawayRow.MonthsAllowed : 3;
                if (layawayRow != null) downpaymentPct = layawayRow.DownpaymentPercentage;

                // 5) Banner: latest payment for CURRENT SLOT only (slotIndex = verifiedPaymentsCount). REJECTED=red, SUBMITTED=yellow, VERIFIED=none.
                tblPaymentsModel latestPaymentForCurrentSlot = null;
                var paymentsByCreated = paymentsList.OrderBy(p => p.CreatedAt).ToList();
                int nextSlot = 0;
                foreach (var p in paymentsByCreated)
                {
                    bool isVerified = string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase);
                    int slot = nextSlot;
                    if (slot == verifiedPaymentsCount)
                        latestPaymentForCurrentSlot = p;
                    if (isVerified)
                        nextSlot++;
                }
                object latestPaymentForProofDto = null;
                if (latestPaymentForCurrentSlot != null && (string.Equals(latestPaymentForCurrentSlot.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase) || string.Equals(latestPaymentForCurrentSlot.PaymentStatus, "REJECTED", StringComparison.OrdinalIgnoreCase)))
                {
                    var proofUrl = latestPaymentForCurrentSlot.ProofImage != null ? (latestPaymentForCurrentSlot.ProofImage.ImagePath + latestPaymentForCurrentSlot.ProofImage.ImageFileName) : null;
                    latestPaymentForProofDto = new
                    {
                        proofImageUrl = proofUrl,
                        referenceNumber = latestPaymentForCurrentSlot.ReferenceNumber,
                        amount = latestPaymentForCurrentSlot.Amount,
                        createdAt = latestPaymentForCurrentSlot.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                        paymentStatus = latestPaymentForCurrentSlot.PaymentStatus,
                        rejectionReason = latestPaymentForCurrentSlot.RejectionReason,
                        allowResubmit = latestPaymentForCurrentSlot.AllowResubmit != 0
                    };
                }

                // 3) Installments remaining: totalInstallments - verifiedPaymentsCount. Not hardcoded.
                decimal amountDue;
                int totalInstallments = 1 + monthsAllowed;
                int installmentsRemaining = totalInstallments - verifiedPaymentsCount;
                if (installmentsRemaining < 0) installmentsRemaining = 0;
                int? installmentMonthNumber = null;
                DateTime? dueDateThisMonth = null;
                // 4) Amount Due (Layaway): A=no verified→downpayment, B=some verified & remaining>0→remaining/installmentsRemaining, C=remaining<=0→0
                if (paymentType == "LAYAWAY")
                {
                    installmentMonthNumber = verifiedPaymentsCount;
                    if (remainingBalanceRecalc <= 0)
                        amountDue = 0;
                    else if (verifiedPaymentsCount == 0)
                        amountDue = Math.Round(order.TotalAmount * (downpaymentPct / 100m), 2);
                    else
                        amountDue = installmentsRemaining > 0 ? Math.Round(remainingBalanceRecalc / installmentsRemaining, 2) : remainingBalanceRecalc;
                    if (verifiedPayments.Any())
                    {
                        var baseDate = verifiedPayments.First().VerifiedAt ?? verifiedPayments.First().CreatedAt;
                        dueDateThisMonth = baseDate.AddMonths(verifiedPaymentsCount);
                    }
                }
                else
                {
                    amountDue = remainingBalanceRecalc;
                }
                var monthlyInstallmentVal = paymentType == "LAYAWAY" && monthsAllowed > 0
                    ? Math.Round((order.TotalAmount - (order.TotalAmount * (downpaymentPct / 100m))) / monthsAllowed, 2)
                    : (decimal?)null;
                int? installmentsRemainingNullable = paymentType == "LAYAWAY" ? (int?)installmentsRemaining : null;
                if (paymentType == "LAYAWAY" && installmentMonthNumber == 0) { installmentMonthNumber = 0; }

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
                    remainingBalance = remainingBalanceRecalc,
                    totalPaidVerified = totalPaidVerified,
                    progressPercent = progressPercent,
                    paymentType = paymentType,
                    amountDue = amountDue,
                    monthsAllowed = paymentType == "LAYAWAY" ? monthsAllowed : (int?)null,
                    verifiedPaymentsCount = paymentType == "LAYAWAY" ? verifiedPaymentsCount : (int?)null,
                    downpaymentPercentage = paymentType == "LAYAWAY" ? (double)downpaymentPct : (double?)null,
                    dueDateThisMonth = dueDateThisMonth.HasValue ? dueDateThisMonth.Value.ToString("MMM d, yyyy") : (string)null,
                    monthlyInstallment = monthlyInstallmentVal,
                    installmentsRemaining = installmentsRemainingNullable,
                    installmentMonthNumber = installmentMonthNumber,
                    hasExistingPayment = hasExistingPayment,
                    latestPaymentForProof = latestPaymentForProofDto,
                    hasSubmittedPayment = paymentsList.Any(p => string.Equals(p.PaymentStatus, "SUBMITTED", StringComparison.OrdinalIgnoreCase)),
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
            var paymentChannel = (request.PaymentChannel ?? "").Trim();

            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username)) return Json(new { success = false, message = "Not authenticated." });
            if (orderId <= 0 || amount <= 0)
                return Json(new { success = false, message = "Invalid order or amount." });
            bool isMeetup = !string.IsNullOrWhiteSpace(meetupDate) && !string.IsNullOrWhiteSpace(meetupTime);
            bool isMeetupChannel = paymentChannel.IndexOf("meetup", StringComparison.OrdinalIgnoreCase) >= 0;
            string refTrimmed = (referenceNumber ?? "").Trim();
            if (refTrimmed.Length > 20)
                return Json(new { success = false, message = "Reference number must be 20 characters or less." });
            if (!isMeetup)
            {
                if (!proofImageId.HasValue || proofImageId.Value <= 0)
                    return Json(new { success = false, message = "Proof of payment is required. Please upload your payment proof first." });
                if (string.IsNullOrEmpty(refTrimmed))
                    return Json(new { success = false, message = "Reference number is required." });
            }
            else
            {
                if (!proofImageId.HasValue || proofImageId.Value <= 0)
                    return Json(new { success = false, message = "Proof of 30% down payment is required. Please upload your payment proof first." });
                if (string.IsNullOrEmpty(refTrimmed))
                    return Json(new { success = false, message = "Reference number for 30% down payment is required." });
            }
            DateTime? meetupDt = null;
            if (isMeetup)
            {
                // Frontend sends date as yyyy-MM-dd from HTML date input; do not accept other formats.
                if (!DateTime.TryParseExact(meetupDate?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var meetupDateParsed))
                    return Json(new { success = false, message = "Valid meetup date is required (yyyy-MM-dd)." });
                if (!DateTime.TryParse(meetupTime, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var t))
                    return Json(new { success = false, message = "Valid meetup time is required (e.g. 10:00 AM)." });
                meetupDt = meetupDateParsed.Date.Add(t.TimeOfDay);
                var today = DateTime.Today;
                var now = DateTime.Now;
                if (meetupDt.Value.Date < today)
                    return Json(new { success = false, message = "Meetup date cannot be in the past." });
                var start = new TimeSpan(10, 0, 0);
                var end = new TimeSpan(17, 0, 0);
                if (meetupDt.Value.TimeOfDay < start || meetupDt.Value.TimeOfDay > end)
                    return Json(new { success = false, message = "Meetup must be between 10:00 AM and 5:00 PM." });
                if (meetupDt.Value.Date == today && meetupDt.Value <= now)
                    return Json(new { success = false, message = "Meetup time cannot be in the past." });
            }
            try
            {
                using (var db = new SolteiraBrandsContext())
                {
                    var user = db.tbl_users.FirstOrDefault(u => u.Username == username);
                    if (user == null) return Json(new { success = false, message = "User not found." });
                    var order = db.tbl_orders.Include("Payments").Include("OrderItems").FirstOrDefault(o => o.Id == orderId && o.UserId == user.Id);
                    if (order == null) return Json(new { success = false, message = "Order not found." });
                    if (order.IsArchived) return Json(new { success = false, message = "Archived orders cannot be modified." });
                    // Apply automatic layaway cancellation before accepting any new payment.
                    AutoCancelLayawayIfMissedPayment(order, db);
                    if (string.Equals(order.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                        return Json(new { success = false, message = "Cannot submit payment for a cancelled order." });
                    // Block only when order is REJECTED or PENDING_APPROVAL. Allow APPROVED, Awaiting Verification, Layaway Active, Pending Payment, Pending Downpayment (resubmit).
                    var blockStatuses = new[] { OrderStatus.Rejected, OrderStatus.PendingApproval };
                    if (blockStatuses.Any(s => string.Equals(order.OrderStatus, s, StringComparison.OrdinalIgnoreCase)))
                        return Json(new { success = false, message = "Order cannot accept payment in this state." });
                    var allowedForPayment = new[] { OrderStatus.Approved, OrderStatus.AwaitingPaymentVerification, OrderStatus.LayawayActive, OrderStatus.PendingPayment, OrderStatus.PendingDownpayment };
                    if (!allowedForPayment.Any(s => string.Equals(order.OrderStatus, s, StringComparison.OrdinalIgnoreCase)))
                        return Json(new { success = false, message = "Order is not in a state that allows payment submission." });
                    var orderPaymentType = (order.PaymentType ?? "FULL").ToUpperInvariant();
                    if (orderPaymentType == "LAYAWAY" && isMeetup)
                        return Json(new { success = false, message = "Layaway orders cannot use Cash through Meetup. Please use GCash or Bank Transfer." });
                    var now = DateTime.Now;
                    var channelDisplay = isMeetup ? "Cash through Meetup" : (string.IsNullOrEmpty(paymentChannel) ? null : paymentChannel);
                    var payment = new tblPaymentsModel
                    {
                        OrderId = order.Id,
                        Amount = amount,
                        PaymentStatus = PaymentStatus.Submitted,
                        ProofImageId = proofImageId,
                        ReferenceNumber = string.IsNullOrEmpty(refTrimmed) ? null : refTrimmed,
                        MeetupDatetime = meetupDt,
                        PaymentChannel = channelDisplay,
                        CreatedAt = now
                    };
                    db.tbl_payments.Add(payment);
                    // SubmitPayment: order status = Awaiting Payment Verification. Applies to FULL and LAYAWAY. No special cases.
                    order.OrderStatus = OrderStatus.AwaitingPaymentVerification;
                    order.LastStatusUpdateAt = now;
                    if (isMeetup || isMeetupChannel)
                        order.DeliveryType = "MEETUP";
                    if (isMeetup && meetupDt.HasValue)
                        order.MeetupDatetime = meetupDt;
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
                    .Where(p => p.Order != null && !p.Order.IsArchived)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();
                var list = payments.Select(p => new
                {
                    paymentId = p.Id,
                    orderId = p.OrderId,
                    orderNumber = p.Order != null ? p.Order.OrderNumber : null,
                    customerName = p.Order != null ? p.Order.CustomerName : null,
                    paymentDate = p.CreatedAt.ToString("o"),
                    paymentDateString = p.CreatedAt.ToString("MM/dd/yyyy hh:mm tt"),
                    paymentOption = p.Order != null ? (string.Equals((p.Order.PaymentType ?? ""), "LAYAWAY", StringComparison.OrdinalIgnoreCase) ? "Layaway" : "Full Payment") : null,
                    paymentMethod = p.PaymentChannel ?? (p.MeetupDatetime.HasValue ? "Cash through Meetup" : (p.Order != null ? p.Order.PaymentMethod : null)),
                    amount = p.Amount,
                    paymentStatus = p.PaymentStatus,
                    referenceNumber = p.ReferenceNumber,
                    proofImageUrl = p.ProofImage != null ? (p.ProofImage.ImagePath + p.ProofImage.ImageFileName) : null,
                    rejectionReason = p.RejectionReason,
                    allowResubmit = p.AllowResubmit != 0,
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
                    .Include("Order.Deliveries")
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
                    paymentOption = string.Equals((order.PaymentType ?? ""), "LAYAWAY", StringComparison.OrdinalIgnoreCase) ? "Layaway" : "Full Payment",
                    paymentMethod = payment.PaymentChannel ?? (payment.MeetupDatetime.HasValue ? "Cash through Meetup" : (order.PaymentMethod ?? "Full Payment")),
                    referenceNumber = payment.ReferenceNumber,
                    paymentStatus = payment.PaymentStatus,
                    rejectionReason = payment.RejectionReason,
                    proofImageUrl = payment.ProofImage != null ? (payment.ProofImage.ImagePath + payment.ProofImage.ImageFileName) : null,
                    createdAtString = payment.CreatedAt.ToString("MM/dd/yyyy hh:mm tt")
                };

                var orderSummary = new
                {
                    id = order.Id,
                    orderNumber = order.OrderNumber,
                    orderStatus = order.OrderStatus,
                    createdAtString = order.CreatedAt.ToString("MM/dd/yyyy hh:mm tt"),
                    customerName = order.CustomerName,
                    email = order.User != null ? order.User.Email : "",
                    contactNumber = order.ContactNumber,
                    deliveryAddress = order.DeliveryAddress,
                    paymentMethod = order.PaymentMethod,
                    totalAmount = order.TotalAmount,
                    items,
                    deliveryType
                };

                object deliveryInfo = null;
                var latestDelivery = order.Deliveries != null && order.Deliveries.Any()
                    ? order.Deliveries.OrderByDescending(d => d.CreatedAt).First()
                    : null;
                if (latestDelivery != null)
                {
                    deliveryInfo = new
                    {
                        courier = latestDelivery.Courier,
                        trackingNumber = latestDelivery.TrackingNumber,
                        dispatchDateFormatted = (latestDelivery.DispatchDate ?? latestDelivery.CreatedAt).ToString("MM/dd/yyyy"),
                        etaFormatted = latestDelivery.Eta.HasValue ? latestDelivery.Eta.Value.ToString("MM/dd/yyyy") : (string)null,
                        deliveryStatus = latestDelivery.DeliveryStatus
                    };
                }

                return Json(new
                {
                    success = true,
                    paymentOverview,
                    orderSummary,
                    paymentHistory,
                    installmentSchedule,
                    meetupSchedule,
                    deliveryInfo,
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
                var ordersInRange = db.tbl_orders.Where(o => !o.IsArchived && o.CreatedAt >= startOfRange && o.CreatedAt < endOfRange).ToList();
                var orderIdsInRange = ordersInRange.Select(o => o.Id).ToList();

                // Only count as "sales" orders that have at least one VERIFIED payment
                var verifiedSaleOrderIds = db.tbl_payments
                    .Where(p => p.PaymentStatus == PaymentStatus.Verified)
                    .Select(p => p.OrderId)
                    .Distinct()
                    .Where(id => orderIdsInRange.Contains(id))
                    .ToList();

                var totalSales = ordersInRange.Where(o => verifiedSaleOrderIds.Contains(o.Id)).Sum(o => o.TotalAmount);
                var productSold = db.tbl_order_items.Where(oi => verifiedSaleOrderIds.Contains(oi.OrderId)).Sum(i => (int?)i.Quantity) ?? 0;
                var pendingOrders = ordersInRange.Count(o => o.OrderStatus == "Pending Approval" || o.OrderStatus == "Pending");
                var approvedOrders = ordersInRange.Count(o => o.OrderStatus == "Approved");
                // Count Completed (and legacy Shipped if any) for dashboard Orders card
                var shippedOrders = ordersInRange.Count(o => o.OrderStatus == "Completed" || o.OrderStatus == "Shipped");

                // Daily sales: only verified-payment orders, by order date
                var lineLabels = new List<string>();
                var lineData = new List<double>();
                var verifiedOrdersInRange = ordersInRange.Where(o => verifiedSaleOrderIds.Contains(o.Id)).ToList();
                for (var i = 0; i < daysInRange; i++)
                {
                    var dayStart = startOfRange.AddDays(i);
                    var dayEnd = dayStart.AddDays(1);
                    var dayTotal = verifiedOrdersInRange.Where(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd).Sum(o => o.TotalAmount);
                    lineLabels.Add(dayStart.ToString("M/d"));
                    lineData.Add((double)dayTotal);
                }
                // Line chart needs at least 2 points to draw; for "today" (single day) duplicate so the segment is visible
                if (daysInRange == 1 && lineLabels.Count == 1)
                {
                    lineLabels.Add(lineLabels[0]);
                    lineData.Add(lineData[0]);
                }

                // Sales by category: only order items from orders with verified payment
                var categories = new[] { "Ring", "Earring", "Necklace", "Bracelet" };
                var pieLabels = new List<string>();
                var pieDataList = new List<double>();
                foreach (var cat in categories)
                {
                    var total = (from oi in db.tbl_order_items
                                 join p in db.tbl_products on oi.ProductId equals p.Id
                                 where p.Category == cat && verifiedSaleOrderIds.Contains(oi.OrderId)
                                 select (decimal?)(oi.Quantity * oi.UnitPrice)).Sum() ?? 0;
                    pieLabels.Add(cat);
                    pieDataList.Add((double)total);
                }

                // Inventory stock: non-archived products with id, name, stockQuantity, initialStock, imageUrl (status = Low Stock if quantity < 3, computed on frontend)
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
                                         initialStock = p.InitialStock,
                                         imageUrl = img != null ? (img.ImagePath + img.ImageFileName) : (string)null
                                     }).ToList();

                // Top selling products: only from orders with verified payment
                var topSold = db.tbl_order_items
                    .Where(oi => oi.ProductId != null && verifiedSaleOrderIds.Contains(oi.OrderId))
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
        public JsonResult UpdateOrderStatus(int id, string status, string rejectionReason = null, bool? restoreStock = null)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Json(new { success = false, message = "Status is required." });
            var statusTrim = status.Trim();
            if (!statusTrim.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                && !statusTrim.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
                && !statusTrim.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Only Approve, Reject, or Cancel is allowed." });

            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.Include("OrderItems").Include("Payments").Include("User").FirstOrDefault(o => o.Id == id);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                if (order.IsArchived) return Json(new { success = false, message = "Archived orders cannot be modified." });
                var current = order.OrderStatus ?? "";

                if (string.Equals(statusTrim, "Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    if (!OrderLifecycleRules.CanTransitionOrder(current, OrderStatus.Rejected))
                        return Json(new { success = false, message = "Invalid order status transition." });
                    var canReject = new[] { OrderStatus.PendingApproval, OrderStatus.Approved };
                    if (!canReject.Any(s => string.Equals(current, s, StringComparison.OrdinalIgnoreCase)))
                        return Json(new { success = false, message = "Only Pending Approval or Approved orders can be rejected." });
                    if (current.Equals("Approved", StringComparison.OrdinalIgnoreCase) && order.Payments != null && order.Payments.Any())
                        return Json(new { success = false, message = "Cannot reject: order already has payment(s). Reject only when no payment exists." });
                    if (string.IsNullOrWhiteSpace(rejectionReason))
                        return Json(new { success = false, message = "Rejection reason is required when rejecting an order." });
                    order.OrderStatus = OrderStatus.Rejected;
                    order.RejectionReason = rejectionReason.Trim();
                    order.LastStatusUpdateAt = DateTime.Now;

                    var shouldRestoreStock = !restoreStock.HasValue || restoreStock.Value;
                    if (shouldRestoreStock)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            if (!item.ProductId.HasValue) continue;
                            var product = db.tbl_products.Find(item.ProductId.Value);
                            if (product != null) { product.StockQuantity += item.Quantity; product.UpdatedAt = DateTime.Now; }
                        }
                    }
                    LogActivity(db, "Order Rejected", "Orders", order.Id, "Order #" + (order.OrderNumber ?? id.ToString()) + "; reason: " + (order.RejectionReason ?? ""));
                }
                else if (string.Equals(statusTrim, "Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    if (!OrderLifecycleRules.CanTransitionOrder(current, OrderStatus.Cancelled))
                        return Json(new { success = false, message = "Invalid order status transition." });
                    order.OrderStatus = OrderStatus.Cancelled;
                    order.RejectionReason = string.IsNullOrWhiteSpace(rejectionReason) ? order.RejectionReason : rejectionReason.Trim();
                    order.LastStatusUpdateAt = DateTime.Now;

                    var shouldRestoreStockOnCancel = !restoreStock.HasValue || restoreStock.Value;
                    if (shouldRestoreStockOnCancel)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            if (!item.ProductId.HasValue) continue;
                            var product = db.tbl_products.Find(item.ProductId.Value);
                            if (product != null) { product.StockQuantity += item.Quantity; product.UpdatedAt = DateTime.Now; }
                        }
                    }
                    LogActivity(db, "Order Cancelled (manual)", "Orders", order.Id, "Order #" + (order.OrderNumber ?? id.ToString()) + (order.RejectionReason != null ? "; reason: " + order.RejectionReason : ""));
                }
                else
                {
                    if (!OrderLifecycleRules.CanTransitionOrder(current, OrderStatus.Approved))
                        return Json(new { success = false, message = "Invalid order status transition." });
                    if (!string.Equals(current, OrderStatus.PendingApproval, StringComparison.OrdinalIgnoreCase))
                        return Json(new { success = false, message = "Only orders with status Pending Approval can be approved." });
                    order.OrderStatus = OrderStatus.Approved;
                    order.RejectionReason = null;
                    order.LastStatusUpdateAt = DateTime.Now;
                    LogActivity(db, "Order Approved", "Orders", order.Id, "Order #" + (order.OrderNumber ?? id.ToString()));
                }

                db.SaveChanges();

                // Notify buyer by email when order is approved (next step: pay).
                // Run email sending in background so it does not block the HTTP response.
                if (string.Equals(order.OrderStatus, OrderStatus.Approved, StringComparison.OrdinalIgnoreCase))
                {
                    var buyerEmail = order.User?.Email;
                    if (!string.IsNullOrWhiteSpace(buyerEmail))
                    {
                        var orderNum = order.OrderNumber ?? id.ToString();
                        var usernameForEmail = order.User != null && !string.IsNullOrWhiteSpace(order.User.Username)
                            ? order.User.Username
                            : "Customer";
                        var subject = usernameForEmail + ", your order #" + orderNum + " has been approved – next step: payment";
                        var payUrl = "https://localhost:44327/SolteiraAndBrands/SolteiraBrandsMyOrders";
                        var body = EmailHelper.BuildOrderApprovedEmailHtml(usernameForEmail, orderNum, payUrl);
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try { EmailHelper.SendEmail(buyerEmail.Trim(), subject, body, isBodyHtml: true); }
                            catch { /* swallow email errors; log inside EmailHelper if needed */ }
                        });
                    }
                }

                return Json(new { success = true, message = "Order status updated.", orderStatus = order.OrderStatus });
            }
        }

        /// <summary>POST: Admin verify or reject payment. Only when payment is SUBMITTED. VERIFIED: recalc totalPaidVerified and remainingBalance from VERIFIED payments only; set order status (Paid/Ready for Meetup when balance 0, else Layaway Active). REJECTED: only mark payment REJECTED; set order to Pending Payment (full) or Pending Downpayment/Layaway Active (layaway) so buyer can resubmit. No change to remaining balance or progress on reject.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdatePaymentStatus(int paymentId, string status, string rejectionReason = null, bool? allowResubmit = null)
        {
            var statusTrim = (status ?? "").Trim().ToUpperInvariant();
            if (statusTrim != PaymentStatus.Verified && statusTrim != PaymentStatus.Rejected)
                return Json(new { success = false, message = "Status must be VERIFIED or REJECTED." });
            if (statusTrim == "REJECTED" && string.IsNullOrWhiteSpace(rejectionReason))
                return Json(new { success = false, message = "Rejection reason is required when rejecting payment." });
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username)) return Json(new { success = false, message = "Not authenticated." });
            using (var db = new SolteiraBrandsContext())
            {
                var adminUser = db.tbl_users.FirstOrDefault(u => u.Username == username);
                if (adminUser == null) return Json(new { success = false, message = "User not found." });
                var payment = db.tbl_payments.Include("Order").Include("Order.Payments").Include("Order.User").FirstOrDefault(p => p.Id == paymentId);
                if (payment == null) return Json(new { success = false, message = "Payment not found." });
                if (payment.Order == null) return Json(new { success = false, message = "Order not found." });
                if (payment.Order.IsArchived) return Json(new { success = false, message = "Archived orders cannot be modified." });
                if (string.Equals(payment.Order.OrderStatus, OrderStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Cannot update payment for a cancelled order." });
                if (!OrderLifecycleRules.CanTransitionPayment(payment.PaymentStatus, statusTrim))
                    return Json(new { success = false, message = "Only SUBMITTED payments can be verified or rejected." });
                var now = DateTime.Now;
                payment.PaymentStatus = statusTrim;
                payment.VerifiedByUserId = adminUser.Id;
                payment.VerifiedAt = now;
                if (statusTrim == "REJECTED")
                {
                    payment.RejectionReason = rejectionReason.Trim();
                    if (allowResubmit.HasValue)
                        payment.AllowResubmit = allowResubmit.Value ? 1 : 0;
                    else
                        payment.AllowResubmit = 1;

                    // Keep order status as Awaiting Payment Verification during resubmission cycle.
                    // Do NOT mutate order.OrderStatus here so that the buyer UI can rely on
                    // the existing Awaiting Payment Verification value + rejection_reason
                    // to drive the "Resubmit Payment" instructions and Pay Now button.
                    payment.Order.LastStatusUpdateAt = now;

                    LogActivity(db, "Payment Rejected", "Payments", payment.Id, "Order #" + (payment.Order.OrderNumber ?? payment.OrderId.ToString()) + "; reason: " + (payment.RejectionReason ?? ""));
                    db.SaveChanges();

                    // Email buyer with retry instructions and button when payment is rejected.
                    var buyerEmail = payment.Order.User != null ? payment.Order.User.Email : null;
                    if (!string.IsNullOrWhiteSpace(buyerEmail))
                    {
                        var orderNum = payment.Order.OrderNumber ?? payment.OrderId.ToString();
                        var usernameForEmail = payment.Order.User != null && !string.IsNullOrWhiteSpace(payment.Order.User.Username)
                            ? payment.Order.User.Username
                            : "Customer";
                        var myOrdersUrl = "https://localhost:44327/SolteiraAndBrands/SolteiraBrandsMyOrders";
                        var subject = usernameForEmail + ", please retry your payment for order #" + orderNum;
                        var body = EmailHelper.BuildPaymentRejectedEmailHtml(usernameForEmail, orderNum, myOrdersUrl, payment.RejectionReason);
                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try { EmailHelper.SendEmail(buyerEmail.Trim(), subject, body, isBodyHtml: true); }
                            catch { /* swallow email errors; log inside EmailHelper if needed */ }
                        });
                    }

                    return Json(new { success = true, message = "Payment rejected.", orderStatus = payment.Order.OrderStatus });
                }
                var orderId = payment.OrderId;
                using (var trans = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Recalculate from VERIFIED payments only (payment is already set to VERIFIED above). Same for FULL and LAYAWAY — manual verification.
                        var verifiedPayments = (payment.Order.Payments ?? new List<tblPaymentsModel>())
                            .Where(p => string.Equals(p.PaymentStatus, "VERIFIED", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        decimal totalPaidVerified = verifiedPayments.Sum(p => p.Amount);
                        decimal remainingBalance = payment.Order.TotalAmount - totalPaidVerified;
                        if (remainingBalance < 0) remainingBalance = 0;

                        var order = db.tbl_orders.Find(orderId);
                        if (order == null) { trans.Rollback(); return Json(new { success = false, message = "Order not found." }); }

                        // For Cash through Meetup / meetup orders, treat verification as a single-payment scenario:
                        // mark the order Ready for Meetup and zero out remaining balance, regardless of partial amount,
                        // and ensure these orders never enter layaway-style flows.
                        var deliveryTypeRaw = (order.DeliveryType ?? string.Empty).Trim();
                        var paymentTypeRaw = (order.PaymentType ?? string.Empty).Trim().ToUpperInvariant();
                        var isMeetupOrder = deliveryTypeRaw.Equals("MEETUP", StringComparison.OrdinalIgnoreCase)
                                            || paymentTypeRaw == "CASH_MEETUP"
                                            || verifiedPayments.Any(p => p.MeetupDatetime.HasValue);

                        if (isMeetupOrder)
                        {
                            order.RemainingBalance = 0;
                            order.OrderStatus = OrderStatus.ReadyForMeetup;
                            order.LastStatusUpdateAt = now;
                        }
                        else
                        {
                            order.RemainingBalance = remainingBalance;
                            order.LastStatusUpdateAt = now;

                            // Non-meetup: fully paid → Paid; else → Layaway Active.
                            if (remainingBalance <= 0)
                            {
                                order.OrderStatus = OrderStatus.Paid;
                            }
                            else
                            {
                                order.OrderStatus = OrderStatus.LayawayActive;
                            }
                        }

                        LogActivity(db, "Order status after verification", "Orders", order.Id, "Order status after verification: " + (order.OrderStatus ?? ""));
                        db.SaveChanges();
                        trans.Commit();

                        // Thank-you email to buyer when payment is verified (order completed from payment perspective).
                        var buyerEmail = payment.Order?.User?.Email;
                        if (!string.IsNullOrWhiteSpace(buyerEmail))
                        {
                            var orderNum = payment.Order.OrderNumber ?? payment.OrderId.ToString();
                            var usernameForEmail = payment.Order.User != null && !string.IsNullOrWhiteSpace(payment.Order.User.Username)
                                ? payment.Order.User.Username
                                : "Customer";
                            var subject = usernameForEmail + ", thank you for your payment for order #" + orderNum;
                            var myOrdersUrl = "https://localhost:44327/SolteiraAndBrands/SolteiraBrandsMyOrders";
                            var body = EmailHelper.BuildPaymentConfirmedEmailHtml(usernameForEmail, orderNum, myOrdersUrl);
                            System.Threading.Tasks.Task.Run(() =>
                            {
                                try { EmailHelper.SendEmail(buyerEmail.Trim(), subject, body, isBodyHtml: true); }
                                catch { /* swallow email errors; log inside EmailHelper if needed */ }
                            });
                        }

                        return Json(new { success = true, message = "Payment verified.", orderStatus = order.OrderStatus });
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
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
                if (order.IsArchived) return Json(new { success = false, message = "Archived orders cannot be modified." });
                if (!order.OrderStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only Approved orders can be revoked." });
                var hasPayments = order.Payments != null && order.Payments.Any();
                if (hasPayments)
                    return Json(new { success = false, message = "Cannot revoke: payment has already been submitted for this order." });
                if (!OrderLifecycleRules.CanTransitionOrder(order.OrderStatus, OrderStatus.PendingApproval))
                    return Json(new { success = false, message = "Cannot revoke approval for this order." });
                order.OrderStatus = OrderStatus.PendingApproval;
                order.LastStatusUpdateAt = DateTime.Now;
                db.SaveChanges();
                LogActivity(db, "Revoke Approval", "Orders", order.Id, "Order #" + (order.OrderNumber ?? orderId.ToString()));
                return Json(new { success = true, message = "Approval revoked. Order is Pending Approval again.", orderStatus = order.OrderStatus });
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
            if (!string.IsNullOrWhiteSpace(dispatchDate))
            {
                if (DateTime.TryParse(dispatchDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d1))
                    dispatchDt = d1.Date;
                else if (DateTime.TryParseExact(dispatchDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d1b))
                    dispatchDt = d1b;
            }
            if (!string.IsNullOrWhiteSpace(eta) && DateTime.TryParse(eta, out var d2)) etaDt = d2;

            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                return Json(new { success = false, message = "Tracking number is required for a delivery shipment." });
            }
            if (!dispatchDt.HasValue)
            {
                return Json(new { success = false, message = "Dispatch date is required for a delivery shipment." });
            }
            if (!etaDt.HasValue)
            {
                return Json(new { success = false, message = "ETA is required for a delivery shipment." });
            }
            if (dispatchDt.HasValue && etaDt.HasValue && etaDt.Value.Date < dispatchDt.Value.Date)
            {
                return Json(new { success = false, message = "ETA cannot be earlier than the dispatch date." });
            }
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.Include("Deliveries").FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                if (order.IsArchived) return Json(new { success = false, message = "Archived orders cannot be modified." });
                var ordStatus = order.OrderStatus ?? "";
                if (!ordStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) && !ordStatus.Equals("Ready for Meetup", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only Paid or Ready for Meetup orders can have delivery created." });
                if (order.Deliveries != null && order.Deliveries.Any())
                    return Json(new { success = false, message = "Delivery already exists for this order." });

                // Normalize tracking number + link ("num||link" or just "num").
                string trackingNumberOnly = null;
                string trackingLink = null;
                if (!string.IsNullOrWhiteSpace(trackingNumber))
                {
                    var parts = trackingNumber.Split(new[] { "||" }, StringSplitOptions.None);
                    trackingNumberOnly = parts.Length > 0 ? parts[0] : null;
                    trackingLink = parts.Length > 1 ? parts[1] : null;
                }

                var courierVal = string.IsNullOrWhiteSpace(courier) ? "LBC" : courier.Trim();
                var delivery = new tblDeliveriesModel
                {
                    OrderId = order.Id,
                    DeliveryType = deliveryTypeVal,
                    Courier = courierVal,
                    TrackingNumber = string.IsNullOrWhiteSpace(trackingNumberOnly) ? null : trackingNumberOnly.Trim(),
                    TrackingLink = string.IsNullOrWhiteSpace(trackingLink) ? null : trackingLink.Trim(),
                    DispatchDate = dispatchDt,
                    Eta = etaDt,
                    DeliveryStatus = DeliveryStatus.InTransit,
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
            var statusTrim = (status ?? "").Trim().ToUpperInvariant();
            if (statusTrim != DeliveryStatus.InTransit && statusTrim != DeliveryStatus.Delivered)
                return Json(new { success = false, message = "Status must be IN_TRANSIT or DELIVERED." });
            var now = DateTime.Now;
            using (var db = new SolteiraBrandsContext())
            {
                var delivery = db.tbl_deliveries.Include("Order").FirstOrDefault(d => d.Id == deliveryId);
                if (delivery == null || delivery.Order == null) return Json(new { success = false, message = "Delivery not found." });
                if (delivery.Order.IsArchived) return Json(new { success = false, message = "Archived orders cannot be modified." });
                var orderStatus = delivery.Order.OrderStatus ?? "";
                if (!OrderLifecycleRules.CanTransitionDelivery(delivery.DeliveryStatus, statusTrim))
                    return Json(new { success = false, message = "Invalid delivery status transition." });
                if (statusTrim == DeliveryStatus.InTransit && !orderStatus.Equals("In Delivery", StringComparison.OrdinalIgnoreCase) && !orderStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Order must be Paid or In Delivery." });
                // Allow marking as Delivered when the order is already In Delivery (normal flow)
                // OR still Paid (for older records where the status wasn't updated correctly).
                if (statusTrim == DeliveryStatus.Delivered
                    && !orderStatus.Equals("In Delivery", StringComparison.OrdinalIgnoreCase)
                    && !orderStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Order must be Paid or In Delivery before marking Delivered." });
                delivery.DeliveryStatus = statusTrim;
                delivery.UpdatedAt = now;
                if (statusTrim == DeliveryStatus.InTransit)
                {
                    delivery.Order.OrderStatus = OrderStatus.InDelivery;
                    delivery.Order.LastStatusUpdateAt = now;
                }
                else if (statusTrim == DeliveryStatus.Delivered)
                {
                    delivery.Order.OrderStatus = OrderStatus.Completed;
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
                if (!OrderLifecycleRules.CanTransitionOrder(order.OrderStatus, OrderStatus.ReadyForMeetup))
                    return Json(new { success = false, message = "Invalid order status transition." });
                order.OrderStatus = OrderStatus.ReadyForMeetup;
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
                if (!OrderLifecycleRules.CanTransitionOrder(order.OrderStatus, OrderStatus.Completed))
                    return Json(new { success = false, message = "Invalid order status transition." });
                order.OrderStatus = OrderStatus.Completed;
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
                    .Where(o => !o.IsArchived
                        && !o.Deliveries.Any()
                        && o.OrderStatus == "Paid"
                        && o.DeliveryType != "MEETUP"
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
                    .Where(d => d.Order != null && !d.Order.IsArchived)
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
                    dispatchDateFormatted = (d.DispatchDate ?? d.CreatedAt).ToString("MM/dd/yyyy"),
                    etaFormatted = d.Eta.HasValue ? d.Eta.Value.ToString("MM/dd/yyyy") : null,
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
                var statusTimeline = new List<object> { new { status = "PENDING", at = delivery.CreatedAt.ToString("MM/dd/yyyy hh:mm tt") } };
                if (!string.Equals(delivery.DeliveryStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
                    statusTimeline.Add(new { status = delivery.DeliveryStatus, at = delivery.UpdatedAt.ToString("MM/dd/yyyy hh:mm tt") });

                var dispatchDt = delivery.DispatchDate ?? delivery.CreatedAt;
                var deliveryOverview = new
                {
                    id = delivery.Id,
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    deliveryStatus = delivery.DeliveryStatus,
                    courier = delivery.Courier,
                    trackingNumber = delivery.TrackingNumber,
                    dispatchDateFormatted = dispatchDt.ToString("MM/dd/yyyy"),
                    dispatchDateIso = dispatchDt.ToString("yyyy-MM-dd"),
                    etaFormatted = delivery.Eta.HasValue ? delivery.Eta.Value.ToString("MM/dd/yyyy") : null,
                    etaIso = delivery.Eta.HasValue ? delivery.Eta.Value.ToString("yyyy-MM-dd") : (string)null,
                    deliveryType = delivery.DeliveryType,
                    createdAtString = delivery.CreatedAt.ToString("MM/dd/yyyy hh:mm tt")
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
                if (delivery.Order.IsArchived)
                    return Json(new { success = false, message = "Archived orders cannot be modified." });
                var currentStatus = (delivery.DeliveryStatus ?? "").Trim();
                if (string.Equals(currentStatus, DeliveryStatus.Delivered, StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Cannot edit a delivered shipment." });

                var now = DateTime.Now;
                var newStatus = (deliveryStatus ?? "").Trim();
                if (!string.IsNullOrEmpty(newStatus) &&
                    (newStatus.Equals(DeliveryStatus.Pending, StringComparison.OrdinalIgnoreCase)
                     || newStatus.Equals(DeliveryStatus.InTransit, StringComparison.OrdinalIgnoreCase)
                     || newStatus.Equals(DeliveryStatus.Delivered, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!OrderLifecycleRules.CanTransitionDelivery(delivery.DeliveryStatus, newStatus))
                        return Json(new { success = false, message = "Invalid delivery status transition." });

                    delivery.DeliveryStatus = newStatus;
                    if (newStatus.Equals(DeliveryStatus.InTransit, StringComparison.OrdinalIgnoreCase))
                    {
                        delivery.Order.OrderStatus = OrderStatus.InDelivery;
                        delivery.Order.LastStatusUpdateAt = now;
                    }
                    else if (newStatus.Equals(DeliveryStatus.Delivered, StringComparison.OrdinalIgnoreCase))
                    {
                        delivery.Order.OrderStatus = OrderStatus.Completed;
                        delivery.Order.LastStatusUpdateAt = now;
                    }
                }
                var status = (delivery.DeliveryStatus ?? "").Trim();
                if (string.Equals(status, DeliveryStatus.Pending, StringComparison.OrdinalIgnoreCase))
                {
                    if (courier != null) delivery.Courier = courier.Trim();
                    if (trackingNumber != null)
                    {
                        // Support both old "num||link" format and new split columns.
                        string tnOnly = null;
                        string tLink = null;
                        if (!string.IsNullOrWhiteSpace(trackingNumber))
                        {
                            var parts = trackingNumber.Split(new[] { "||" }, StringSplitOptions.None);
                            tnOnly = parts.Length > 0 ? parts[0] : null;
                            tLink = parts.Length > 1 ? parts[1] : null;
                        }
                        delivery.TrackingNumber = string.IsNullOrWhiteSpace(tnOnly) ? null : tnOnly.Trim();
                        if (!string.IsNullOrWhiteSpace(tLink))
                        {
                            delivery.TrackingLink = tLink.Trim();
                        }
                    }
                    DateTime d;
                    if (!string.IsNullOrWhiteSpace(dispatchDate) && DateTime.TryParse(dispatchDate, out d))
                        delivery.DispatchDate = d;
                    if (!string.IsNullOrWhiteSpace(eta) && DateTime.TryParse(eta, out d))
                        delivery.Eta = d;
                }
                else if (string.Equals(status, DeliveryStatus.InTransit, StringComparison.OrdinalIgnoreCase))
                {
                    if (trackingNumber != null)
                    {
                        string tnOnly = null;
                        string tLink = null;
                        if (!string.IsNullOrWhiteSpace(trackingNumber))
                        {
                            var parts = trackingNumber.Split(new[] { "||" }, StringSplitOptions.None);
                            tnOnly = parts.Length > 0 ? parts[0] : null;
                            tLink = parts.Length > 1 ? parts[1] : null;
                        }
                        delivery.TrackingNumber = string.IsNullOrWhiteSpace(tnOnly) ? null : tnOnly.Trim();
                        if (!string.IsNullOrWhiteSpace(tLink))
                        {
                            delivery.TrackingLink = tLink.Trim();
                        }
                    }
                }
                delivery.UpdatedAt = now;
                db.SaveChanges();
                LogActivity(db, "Delivery Updated", "Deliveries", delivery.Id, "Order #" + (delivery.Order.OrderNumber ?? delivery.OrderId.ToString()));
                return Json(new { success = true, message = "Delivery updated.", deliveryStatus = delivery.DeliveryStatus, orderStatus = delivery.Order.OrderStatus });
            }
        }

        /// <summary>GET: Orders for Delivery Meetup page. order_status = Ready for Meetup OR Meetup Rescheduled.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetOrdersForDeliveryMeetup()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var allowedStatuses = new[]
                {
                    OrderStatus.ReadyForMeetup,
                    OrderStatus.MeetupRescheduled,
                    OrderStatus.NoShow,
                    OrderStatus.Completed,
                    OrderStatus.Cancelled
                };

                var orders = db.tbl_orders
                    .Include("Payments")
                    .Where(o =>
                        !o.IsArchived
                        && allowedStatuses.Contains(o.OrderStatus)
                        && (o.DeliveryType == "MEETUP"
                            || db.tbl_meetup_outcomes.Any(m => m.OrderId == o.Id)))
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList()
                    .Select(o =>
                    {
                        var scheduledAt = o.MeetupDatetime
                            ?? (o.Payments != null ? o.Payments.FirstOrDefault(p => p.MeetupDatetime.HasValue)?.MeetupDatetime : null);
                        return new
                        {
                            id = o.Id,
                            orderNumber = o.OrderNumber,
                            customerName = o.CustomerName,
                            deliveryAddress = o.DeliveryAddress,
                            contactNumber = o.ContactNumber,
                            totalAmount = o.TotalAmount,
                            orderStatus = o.OrderStatus,
                            createdAt = o.CreatedAt,
                            scheduledAtFormatted = scheduledAt.HasValue ? scheduledAt.Value.ToString("MM/dd/yyyy hh:mm tt") : (string)null,
                            scheduledAt = scheduledAt
                        };
                    })
                    .ToList();

                return Json(new { success = true, data = orders }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// GET: Admin — meetup order detail for View modal.
        /// Includes:
        /// - Order summary, customer, meetup schedule (location), scheduled date/time.
        /// - Latest meetup outcome (Completed / No Show / Cancelled) with timestamp and optional receipt image.
        /// Allowed statuses: Ready for Meetup, Meetup Rescheduled, No Show, Completed, Cancelled.
        /// </summary>
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
                var allowedStatuses = new[] { OrderStatus.ReadyForMeetup, OrderStatus.MeetupRescheduled, OrderStatus.NoShow, OrderStatus.Completed, OrderStatus.Cancelled };
                if (order == null || !allowedStatuses.Any(s => string.Equals(order.OrderStatus, s, StringComparison.OrdinalIgnoreCase)))
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                var meetupRow = db.tbl_meetup_settings.OrderByDescending(x => x.Id).FirstOrDefault();
                var scheduledAt = order.MeetupDatetime ?? (order.Payments != null ? order.Payments.FirstOrDefault(p => p.MeetupDatetime.HasValue)?.MeetupDatetime : null);
                var items = order.OrderItems.Select(i => new { productName = i.ProductName, quantity = i.Quantity, unitPrice = i.UnitPrice }).ToList();

                // Latest meetup outcome (Completed / No Show / Cancelled) with receipt if any.
                var latestOutcome = db.tbl_meetup_outcomes
                    .Include("ReceiptImage")
                    .Where(m => m.OrderId == order.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();
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
                var meetupOutcomeStatus = latestOutcome != null ? latestOutcome.Status : null;
                var meetupOutcomeAtFormatted = latestOutcome != null ? latestOutcome.CreatedAt.ToString("MMM dd, yyyy hh:mm tt") : null;
                string meetupReceiptImageUrl = null;
                if (latestOutcome != null && latestOutcome.ReceiptImage != null)
                {
                    meetupReceiptImageUrl = (latestOutcome.ReceiptImage.ImagePath ?? "") + (latestOutcome.ReceiptImage.ImageFileName ?? "");
                }
                return Json(new
                {
                    success = true,
                    orderSummary,
                    meetupSchedule,
                    scheduledAtFormatted,
                    meetupOutcomeStatus,
                    meetupOutcomeAtFormatted,
                    meetupReceiptImageUrl
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

        /// <summary>POST: Update meetup outcome. Allowed when order is Ready for Meetup or Meetup Rescheduled. COMPLETED requires receiptImageId; NO_SHOW sets order to No Show (enables Reschedule); CANCELLED sets order Cancelled.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdateMeetupOutcome(int orderId, string status, int? receiptImageId = null, string cancellationReason = null, bool? restoreStock = null)
        {
            var statusTrim = (status ?? "").Trim().ToUpperInvariant();
            if (statusTrim != "COMPLETED" && statusTrim != "NO_SHOW" && statusTrim != "CANCELLED")
                return Json(new { success = false, message = "Status must be COMPLETED, NO_SHOW, or CANCELLED." });
            if (statusTrim == "COMPLETED" && (!receiptImageId.HasValue || receiptImageId.Value <= 0))
                return Json(new { success = false, message = "Receipt upload is required for Completed." });
            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.Include("OrderItems").FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                var allowedFrom = new[] { OrderStatus.ReadyForMeetup, OrderStatus.MeetupRescheduled, OrderStatus.NoShow };
                if (!allowedFrom.Any(s => string.Equals(order.OrderStatus, s, StringComparison.OrdinalIgnoreCase)))
                    return Json(new { success = false, message = "Only Ready for Meetup, Meetup Rescheduled, or No Show orders can be updated." });
                var now = DateTime.Now;
                if (statusTrim == "COMPLETED")
                {
                    if (!OrderLifecycleRules.CanTransitionOrder(order.OrderStatus, OrderStatus.Completed))
                        return Json(new { success = false, message = "Cannot transition to Completed from current status." });
                    order.OrderStatus = OrderStatus.Completed;
                    order.LastStatusUpdateAt = now;
                    db.tbl_meetup_outcomes.Add(new tblMeetupOutcomeModel { OrderId = orderId, Status = "COMPLETED", ReceiptImageId = receiptImageId, CreatedAt = now });
                    LogActivity(db, "Meetup Completed", "MeetupOutcomes", orderId, "Order #" + (order.OrderNumber ?? orderId.ToString()) + "; receipt uploaded.");
                }
                else if (statusTrim == "NO_SHOW")
                {
                    if (!OrderLifecycleRules.CanTransitionOrder(order.OrderStatus, OrderStatus.NoShow))
                        return Json(new { success = false, message = "Cannot transition to No Show from current status." });
                    order.OrderStatus = OrderStatus.NoShow;
                    order.LastStatusUpdateAt = now;
                    db.tbl_meetup_outcomes.Add(new tblMeetupOutcomeModel { OrderId = orderId, Status = "NO_SHOW", CreatedAt = now });
                    LogActivity(db, "Meetup No Show", "MeetupOutcomes", orderId, "Order #" + (order.OrderNumber ?? orderId.ToString()));
                }
                else
                {
                    if (!OrderLifecycleRules.CanTransitionOrder(order.OrderStatus, OrderStatus.Cancelled))
                        return Json(new { success = false, message = "Cannot cancel from current status." });
                    order.OrderStatus = OrderStatus.Cancelled;
                    order.RejectionReason = string.IsNullOrWhiteSpace(cancellationReason)
                        ? order.RejectionReason
                        : cancellationReason.Trim();
                    order.LastStatusUpdateAt = now;

                    // Optional stock restoration on cancel, same semantics as manual order cancel:
                    // default is to restore stock unless explicitly disabled.
                    var shouldRestoreStockOnCancel = !restoreStock.HasValue || restoreStock.Value;
                    if (shouldRestoreStockOnCancel && order.OrderItems != null)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            if (!item.ProductId.HasValue) continue;
                            var product = db.tbl_products.Find(item.ProductId.Value);
                            if (product != null)
                            {
                                product.StockQuantity += item.Quantity;
                                product.UpdatedAt = now;
                            }
                        }
                    }

                    db.tbl_meetup_outcomes.Add(new tblMeetupOutcomeModel
                    {
                        OrderId = orderId,
                        Status = "CANCELLED",
                        CreatedAt = now
                    });
                    LogActivity(
                        db,
                        "Meetup Cancelled",
                        "MeetupOutcomes",
                        orderId,
                        "Order #" + (order.OrderNumber ?? orderId.ToString())
                        + (order.RejectionReason != null ? "; reason: " + order.RejectionReason : ""));
                }
                db.SaveChanges();
                return Json(new { success = true, message = "Meetup outcome updated.", orderStatus = order.OrderStatus });
            }
        }

        /// <summary>POST: Reschedule meetup. Only when order status is No Show. Saves new meetupDate/meetupTime and sets order to Meetup Rescheduled.</summary>
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult RescheduleMeetup(int orderId, string meetupDate, string meetupTime)
        {
            if (string.IsNullOrWhiteSpace(meetupDate) || string.IsNullOrWhiteSpace(meetupTime))
                return Json(new { success = false, message = "Meetup date and time are required." });
            if (!DateTime.TryParseExact(meetupDate.Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var meetupDateParsed))
                return Json(new { success = false, message = "Valid meetup date is required (yyyy-MM-dd)." });
            if (!DateTime.TryParse(meetupTime, System.Globalization.CultureInfo.GetCultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out var t))
                return Json(new { success = false, message = "Valid meetup time is required (e.g. 10:00 AM)." });
            var meetupDt = meetupDateParsed.Date.Add(t.TimeOfDay);
            var today = DateTime.Today;
            var now = DateTime.Now;
            if (meetupDt.Date < today)
                return Json(new { success = false, message = "Meetup date cannot be in the past." });
            var start = new TimeSpan(10, 0, 0);
            var end = new TimeSpan(17, 0, 0);
            if (meetupDt.TimeOfDay < start || meetupDt.TimeOfDay > end)
                return Json(new { success = false, message = "Meetup must be between 10:00 AM and 5:00 PM." });
            if (meetupDt.Date == today && meetupDt <= now)
                return Json(new { success = false, message = "Meetup time cannot be in the past." });

            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                if (!string.Equals(order.OrderStatus, OrderStatus.NoShow, StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Only No Show orders can be rescheduled." });
                if (!OrderLifecycleRules.CanTransitionOrder(order.OrderStatus, OrderStatus.MeetupRescheduled))
                    return Json(new { success = false, message = "Cannot reschedule from current status." });
                order.MeetupDatetime = meetupDt;
                order.OrderStatus = OrderStatus.MeetupRescheduled;
                order.LastStatusUpdateAt = now;
                LogActivity(db, "Meetup Rescheduled", "Orders", orderId, "Order #" + (order.OrderNumber ?? orderId.ToString()) + "; new time: " + meetupDt.ToString("MMM dd, yyyy hh:mm tt"));
                db.SaveChanges();
                return Json(new { success = true, message = "Meetup rescheduled.", orderStatus = order.OrderStatus, scheduledAtFormatted = meetupDt.ToString("MM/dd/yyyy hh:mm tt") });
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
                    .Where(o => (o.OrderStatus == "Pending Approval" || o.OrderStatus == "Pending" || o.OrderStatus == "Pending Payment" || o.OrderStatus == "Pending Downpayment" || o.OrderStatus == "Approved" || o.OrderStatus == "Awaiting Payment Verification") && o.CreatedAt < cutoff)
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
                        date = x.date.ToString("MM/dd/yyyy"),
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

        // ----- USERS (Admin): registered users list -----
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpGet]
        public JsonResult GetUsersForAdmin()
        {
            using (var db = new SolteiraBrandsContext())
            {
                var raw = db.tbl_users
                    .Include("Role")
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new
                    {
                        id = u.Id,
                        username = u.Username,
                        email = u.Email,
                        roleName = u.Role != null ? u.Role.Name : "UNKNOWN",
                        isActive = u.IsActive,
                        isVerified = u.IsVerified,
                        joinedAt = u.CreatedAt
                    })
                    .ToList();

                var list = raw.Select(u => new
                {
                    id = u.id,
                    username = u.username,
                    email = u.email,
                    roleName = u.roleName,
                    isActive = u.isActive,
                    isVerified = u.isVerified,
                    joinedAtString = u.joinedAt.ToString("MM/dd/yyyy")
                }).ToList();

                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
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