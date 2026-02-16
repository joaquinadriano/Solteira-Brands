using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

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

        // GET: SolteiraAndBrands
        public ActionResult AdminHomePage() 
        {
            return View();
        }
        public ActionResult AdminLoginPage() 
        {
            return View();
        }
        public ActionResult AdminPasswordChangePage() 
        {
            return View();
        }
        public ActionResult AdminPasswordChangePage1() 
        {
            return View();
        }
        public ActionResult AdminDashboardPage() 
        {
            return View();
        }

        public ActionResult AdminOrdersPage()
        {
            return View();
        }
        public ActionResult AdminOrderDetailsPage()
        {
            return View();
        }
        public ActionResult AdminOrdersArchivePage()
        {
            return View();
        }
        public ActionResult AdminPaymentPage()
        {
            return View();
        }
        public ActionResult AdminDeliveryShipmentsPage()
        {
            return View();
        }
        public ActionResult AdminDeliveryMeetupPage()
        {
            return View();
        }

        public ActionResult AdminInventoryPage()
        {
            return View();
        }

        public ActionResult AdminReportsPage()
        {
            return View();
        }

        public ActionResult AdminLogsPage()
        {
            return View();
        }

        public ActionResult AdminUsersPage()
        {
            return View();
        }
    }
}