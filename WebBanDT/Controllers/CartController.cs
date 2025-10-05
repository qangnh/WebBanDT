using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebBanDT.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        public ActionResult GioHang()
        {
            return View();
        }
        public ActionResult GiaoHang()
        {
            return View();
        }
    }
}