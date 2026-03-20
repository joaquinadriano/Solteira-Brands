using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Solteira_Brands
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Dedicated route for email verification after login.
            // Example: /verify?email=user@example.com
            routes.MapRoute(
                name: "VerifyEmail",
                url: "verify",
                defaults: new { controller = "Account", action = "Verify" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "SolteiraAndBrands", action = "SolteiraBrandsHome", id = UrlParameter.Optional }
            );
        }
    }
}
