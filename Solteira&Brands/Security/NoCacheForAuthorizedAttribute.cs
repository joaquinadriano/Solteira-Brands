using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Solteira_Brands.Security
{
    /// <summary>
    /// Prevents browser back-button / bfcache snapshots for authenticated pages.
    /// Applies ONLY to GET/HEAD responses that are protected by [Authorize]/[BuyerAuthorize].
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class NoCacheForAuthorizedAttribute : ActionFilterAttribute
    {
        // Use OnActionExecuting so ActionDescriptor / ControllerDescriptor are available.
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null) return;

            var request = filterContext.HttpContext.Request;
            if (request == null) return;

            var method = request.HttpMethod ?? "";
            if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                !method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Skip actions explicitly marked as public.
            var actionDescriptor = filterContext.ActionDescriptor;
            var controllerType = filterContext.Controller != null ? filterContext.Controller.GetType() : null;

            var hasAllowAnonymous =
                (actionDescriptor != null &&
                 actionDescriptor.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any()) ||
                (controllerType != null &&
                 controllerType.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any());

            if (hasAllowAnonymous) return;

            // Only apply to actions/controllers that require authentication/role checks.
            var hasAuthorize =
                (actionDescriptor != null &&
                 actionDescriptor.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any()) ||
                (controllerType != null &&
                 controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any());

            if (!hasAuthorize) return;

            var response = filterContext.HttpContext.Response;
            if (response == null) return;

            response.Cache.SetCacheability(HttpCacheability.NoCache);
            response.Cache.SetNoStore();
            response.Cache.SetExpires(DateTime.UtcNow.AddYears(-1));
            response.Cache.SetMaxAge(TimeSpan.Zero);

            response.AppendHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            response.AppendHeader("Pragma", "no-cache");
        }
    }
}

