using System;
using System.Linq;
using System.Web.Mvc;
using Solteira_Brands.Models.Context;

namespace Solteira_Brands.Security
{
    /// <summary>
    /// Enforces mandatory password change for system-managed accounts.
    /// Applies to authenticated ADMIN/MANAGER/STAFF users with must_change_password = 1.
    /// </summary>
    public class ForcePasswordChangeFilterAttribute : ActionFilterAttribute
    {
        private static bool IsAllowedAction(string controller, string action)
        {
            if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(action, "ChangePassword", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(action, "Logout", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(controller, "SolteiraAndBrands", StringComparison.OrdinalIgnoreCase)
                && string.Equals(action, "SolteiraBrandsChangePassword", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            if (filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true) ||
                filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true))
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            var user = filterContext.HttpContext.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            var username = user.Identity.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            var controller = (string)filterContext.RouteData.Values["controller"] ?? string.Empty;
            var action = (string)filterContext.RouteData.Values["action"] ?? string.Empty;
            if (IsAllowedAction(controller, action))
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            using (var db = new SolteiraBrandsContext())
            {
                var account = db.tbl_users
                    .Include("Role")
                    .FirstOrDefault(u => u.Username == username && u.IsActive);

                if (account == null)
                {
                    base.OnActionExecuting(filterContext);
                    return;
                }

                var role = account.Role?.Name ?? string.Empty;
                var isSystemRole = role == "ADMIN" || role == "MANAGER" || role == "STAFF";

                if (isSystemRole && account.MustChangePassword)
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new System.Web.Routing.RouteValueDictionary(
                            new { controller = "SolteiraAndBrands", action = "SolteiraBrandsChangePassword" }));
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
