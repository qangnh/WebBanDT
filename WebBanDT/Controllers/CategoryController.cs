using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebBanDT.App_Start
{
    public class CategoryController : Controller
    {
        // GET: Category
        public ActionResult Phones()
        {
            return View();
        }
        public ActionResult Laptop()
        {
            return View();
        }
        public ActionResult Screen()
        {
            return View();
        }
        public ActionResult Tablet()
        {
            return View();
        }
        public ActionResult Sound()
        {
            return View();
        }
        public ActionResult Watch()
        {
            return View();
        }
    }
}