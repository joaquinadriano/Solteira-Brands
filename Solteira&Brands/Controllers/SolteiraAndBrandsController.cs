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

        // ----- ADMIN / INVENTORY PAGES (require inventory role) -----
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
        public ActionResult AdminOrdersPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminOrderDetailsPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminOrdersArchivePage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminPaymentPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminDeliveryShipmentsPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminDeliveryMeetupPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminInventoryPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminReportsPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminLogsPage() { return View(); }
        [Authorize(Roles = "ADMIN,MANAGER,STAFF")]
        public ActionResult AdminUsersPage() { return View(); }

        // ----- E-COMMERCE PAGES -----
        public ActionResult SolteiraBrandsAbout()
        {
            return View();
        }
        public ActionResult SolteiraBrandsAccountSettings()
        {
            return View();
        }
        public ActionResult SolteiraBrandsCart()
        {
            return View();
        }
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
        public ActionResult SolteiraBrandsMyOrders()
        {
            return View();
        }

        // ----- IMAGE UPLOAD (tbl_images; files to ~/Content/images/productsupload/) -----
        [HttpPost]
        public JsonResult UploadProductImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "No file uploaded." });
            var fileName = file.FileName ?? "";
            if (!fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Only JPG/JPEG images are allowed." });
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
    }
}