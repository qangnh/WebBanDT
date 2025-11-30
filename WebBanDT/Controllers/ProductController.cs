using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDT.Models;
using WebBanDT.Models.ViewModels;
using System.Data.Entity;


namespace WebBanDT.Controllers
{
    public class ProductController : Controller
    {
        // GET: /Product/
        public ActionResult Product()
        {
            return View();
        }

        // GET: /Product/Details/5
        public ActionResult Details(int id)
        {
            using (var db = new WebBanDTEntities())
            {
                var product = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductColors)
                    .Include(p => p.ProductVersions)
                    .Where(p => p.ProductID == id && p.IsActive == true)
                    .Select(p => new ProductVM
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        ProductImage = p.ProductImage,
                        ProductPrice = p.ProductPrice,
                        ProductDescription = p.ProductDescription,
                        CategoryName = p.Category.CategoryName,
                        StockQuantity = p.StockQuantity,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.IsActive,

                        // ✅ list phiên bản (dùng luôn entity ProductVersion)
                        Versions = p.ProductVersions.ToList(),

                        // ✅ list màu sắc (dùng luôn entity ProductColor, có ColorImage)
                        Colors = p.ProductColors.ToList()
                    })
                    .FirstOrDefault();

                if (product == null)
                {
                    return HttpNotFound();
                }

                // ⭐ TÍNH TỔNG SỐ LƯỢNG TRONG GIỎ HÀNG ĐỂ HIỂN THỊ LÊN HEADER
                int cartCount = 0;
                if (Session["UserID"] != null)
                {
                    int userId = Convert.ToInt32(Session["UserID"]);

                    cartCount = db.CartItems
                                  .Where(ci => ci.Cart.UserID == userId &&
                                               (ci.Cart.IsCheckedOut == false || ci.Cart.IsCheckedOut == null))
                                  .Sum(ci => (int?)ci.Quantity) ?? 0;
                }

                ViewBag.CartCount = cartCount;

                return View(product);
            }
        }
    }
}