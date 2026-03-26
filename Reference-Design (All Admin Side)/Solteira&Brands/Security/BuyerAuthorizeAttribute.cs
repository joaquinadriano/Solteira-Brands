using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Solteira_Brands.Security
{
    /// <summary>
    /// Authorize attribute for BUYER-only storefront actions.
    /// - Allows only authenticated users in BUYER role.
    /// - Redirects ADMIN / MANAGER / STAFF to AdminDashboardPage.
    /// - Redirects anonymous users to SolteiraBrandsLogin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class BuyerAuthorizeAttribute : AuthorizeAttribute
    {
        public BuyerAuthorizeAttribute()
        {
            Roles = "BUYER";
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));

            HttpContextBase httpContext = filterContext.HttpContext;
            var user = httpContext?.User;

            if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
            {
                // Authenticated but not BUYER – redirect backend roles to dashboard
                if (user.IsInRole("ADMIN") || user.IsInRole("MANAGER") || user.IsInRole("STAFF"))
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary(
                            new
                            {
                                controller = "SolteiraAndBrands",
                                action = "AdminDashboardPage"
                            }));
                    return;
                }
            }

            // Anonymous or non-matching role – send to storefront login
            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary(
                    new
                    {
                        controller = "SolteiraAndBrands",
                        action = "SolteiraBrandsLogin"
                    }));
        }
    }
}

