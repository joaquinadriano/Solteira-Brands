using System.Web;
using System.Web.Mvc;
using Solteira_Brands.Security;

namespace Solteira_Brands
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // Back-button hardening: never cache authenticated pages.
            // Applies internally only to GET/HEAD actions protected by [Authorize]/[BuyerAuthorize].
            filters.Add(new NoCacheForAuthorizedAttribute());
        }
    }
}
