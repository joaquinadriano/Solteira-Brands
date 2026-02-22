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
using Solteira_Brands.Models;
using Solteira_Brands.Models.Context;
using Solteira_Brands.Security;

namespace Solteira_Brands.Controllers
{
    public class SolteiraAndBrandsController : Controller
    {
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
        [BuyerAuthorize]
        public ActionResult SolteiraBrandsAccountSettings()
        {
            return View();
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
                                    img = img != null ? (img.ImagePath + img.ImageFileName) : null,
                                    isBestSeller = p.IsBestSeller
                                }).ToList();

                return Json(new { success = true, data = products }, JsonRequestBehavior.AllowGet);
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

                var now = DateTime.Now;
                var order = new tblOrdersModel
                {
                    UserId = user.Id,
                    OrderNumber = "", // set after insert
                    CustomerName = (request.CustomerName ?? "").Trim(),
                    ContactNumber = (request.ContactNumber ?? "").Trim(),
                    DeliveryAddress = (request.DeliveryAddress ?? "").Trim(),
                    PaymentMethod = (request.PaymentMethod ?? "Full Payment").Trim(),
                    TotalAmount = request.TotalAmount,
                    OrderStatus = "Pending",
                    CreatedAt = now
                };
                if (string.IsNullOrEmpty(order.CustomerName) || string.IsNullOrEmpty(order.ContactNumber) || string.IsNullOrEmpty(order.DeliveryAddress))
                    return Json(new { success = false, message = "Customer name, contact number, and delivery address are required." });

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
                var list = db.tbl_orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new
                    {
                        id = o.Id,
                        orderNumber = o.OrderNumber,
                        orderDate = o.CreatedAt,
                        itemCount = o.OrderItems.Count,
                        deliveryAddress = o.DeliveryAddress,
                        totalAmount = o.TotalAmount,
                        orderStatus = o.OrderStatus,
                        customerName = o.CustomerName,
                        contactNumber = o.ContactNumber,
                        paymentMethod = o.PaymentMethod
                    }).ToList()
                    .Select(x => new
                    {
                        x.id,
                        x.orderNumber,
                        orderDateString = x.orderDate.ToString("MM/dd/yyyy"),
                        itemCount = x.itemCount,
                        x.deliveryAddress,
                        x.totalAmount,
                        x.orderStatus,
                        x.customerName,
                        x.contactNumber,
                        x.paymentMethod
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
                    .FirstOrDefault(o => o.Id == id);
                if (order == null) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

                var items = order.OrderItems.Select(i => new
                {
                    productName = i.ProductName,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    imageUrl = i.ImageUrl
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
                        items = items
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

                var list = db.tbl_orders
                    .Where(o => o.UserId == user.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new
                    {
                        orderId = o.Id,
                        orderNumber = o.OrderNumber,
                        status = o.OrderStatus,
                        deliveryStatus = "Preparing",
                        createdAt = o.CreatedAt,
                        total = o.TotalAmount,
                        items = o.OrderItems.Select(i => new { name = i.ProductName, qty = i.Quantity, price = i.UnitPrice, img = i.ImageUrl }).ToList()
                    }).ToList();
                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
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
                var shippedOrders = ordersInRange.Count(o => o.OrderStatus == "Shipped");

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

        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        [HttpPost]
        public JsonResult UpdateOrderStatus(int id, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Json(new { success = false, message = "Status is required." });
            var allowed = new[] { "Pending", "Approved", "Rejected", "Fully Paid", "Shipped" };
            if (!allowed.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Invalid status." });

            using (var db = new SolteiraBrandsContext())
            {
                var order = db.tbl_orders.FirstOrDefault(o => o.Id == id);
                if (order == null) return Json(new { success = false, message = "Order not found." });
                order.OrderStatus = status.Trim();
                db.SaveChanges();
                LogActivity(db, "Order " + order.OrderStatus, "Orders", order.Id, "Order #" + (order.OrderNumber ?? id.ToString()));
                return Json(new { success = true, message = "Order status updated.", orderStatus = order.OrderStatus });
            }
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

        // ----- IMAGE UPLOAD (tbl_images; files to ~/Content/images/productsupload/) -----
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

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);
            string uploadPath = Server.MapPath("~/Content/images/productsupload");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            file.SaveAs(Path.Combine(uploadPath, uniqueFileName));

            const string imagePath = "/Content/images/productsupload/";
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

        [BuyerAuthorize]
        public ActionResult SolteiraBrandsChangePassword()
        {
            return View("~/Views/SolteiraBrandsAccount/SolteiraBrandsChangePassword.cshtml");
        }
    }
}