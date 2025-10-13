using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDT.Models;

namespace WebBanDT.Controllers
{
    public class CartController : Controller
    {
        public ActionResult GioHang()
        {
            return View();
        }

        public ActionResult GiaoHang()
        {
            return View();
        }

        // GET: ThongTin
        public ActionResult ThongTin()
        {
            var model = Session["OrderInfo"] as OrderInfoViewModel ?? new OrderInfoViewModel();
            return View(model);
        }

        // POST: ThongTin
        [HttpPost]
        public ActionResult ThongTin(OrderInfoViewModel model)
        {
            if (ModelState.IsValid)
            {
                Session["OrderInfo"] = model; // lưu dữ liệu vào session
                return RedirectToAction("ThanhToan");
            }
            return View(model);
        }

        // GET: ThanhToan
        public ActionResult ThanhToan()
        {
            var model = Session["OrderInfo"] as OrderInfoViewModel ?? new OrderInfoViewModel();
            return View(model);
        }
    }
}
