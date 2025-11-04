using System.Web.Mvc;

namespace WebBanDT.Areas.AdminHome
{
    public class AdminHomeAreaRegistration : AreaRegistration
    {
        public override string AreaName => "AdminHome";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "AdminHome_default",
                "AdminHome/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "WebBanDT.Areas.AdminHome.Controllers" }
            );
        }
    }
}
