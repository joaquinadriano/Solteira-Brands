using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
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