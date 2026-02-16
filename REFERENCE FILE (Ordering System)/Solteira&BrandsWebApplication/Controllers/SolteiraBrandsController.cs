using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Solteira_BrandsWebApplication.Controllers
{
    public class SolteiraBrandsController : Controller
    {
        // GET: SolteiraBrands
        public ActionResult SolteiraBrandsHome()
        {
            Session["Username"] = "TestUser";
            return View();
        }

        public ActionResult SolteiraBrandsJewelries()
        {
            return View();
        }

        public ActionResult SolteiraBrandsAbout()
        {
            return View();
        }

        public ActionResult SolteiraBrandsFaq()
        {
            return View();
        }

        public ActionResult SolteiraBrandsAccountSettings()
        {
            return View();
        }

        public ActionResult SolteiraBrandsMyOrders()
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
    }
}