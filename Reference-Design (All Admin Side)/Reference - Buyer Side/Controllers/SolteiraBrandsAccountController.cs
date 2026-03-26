using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Solteira_BrandsWebApplication.Controllers
{
    public class SolteiraBrandsAccountController : Controller
    {
        // GET: Account
        public ActionResult SolteiraBrandsLogin()
        {
            return View();
        }

        public ActionResult SolteiraBrandsRegister()
        {
            return View();
        }

        public ActionResult SolteiraBrandsOTP()
        {
            return View();
        }

        public ActionResult SolteiraBrandsForgotPassword()
        {
            return View();
        }

        public ActionResult SolteiraBrandsNewPassword()
        {
            return View();
        }
    }
}