using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using Solteira_Brands.Services;

namespace Solteira_Brands
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Enforce payment deadlines (auto-cancel + stock restoration) every 5 minutes.
            OrderDeadlineService.StartAutoCancellationJob();
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null) return;

            try
            {
                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket == null || ticket.Expired || string.IsNullOrEmpty(ticket.UserData))
                {
                    // Expired or invalid auth cookie: keep user unauthenticated and clear cookie.
                    try { FormsAuthentication.SignOut(); } catch { }
                    return;
                }

                var parts = ticket.UserData.Split('|');
                var username = parts.Length > 0 ? parts[0] : ticket.Name;
                var role = parts.Length > 1 ? parts[1] : "";

                var identity = new GenericIdentity(ticket.Name, "Forms");
                var principal = new GenericPrincipal(identity, new[] { role });
                HttpContext.Current.User = principal;
            }
            catch
            {
                // Invalid ticket; leave User as unauthenticated
            }
        }
    }
}
