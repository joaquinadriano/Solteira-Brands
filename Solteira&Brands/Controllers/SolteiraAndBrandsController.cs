using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Solteira_Brands.Controllers
{
    public class SolteiraAndBrandsController : Controller
    {
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
    }
}