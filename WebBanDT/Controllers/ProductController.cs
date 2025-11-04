using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDT.Models;
using WebBanDT.Models.ViewModels;

namespace WebBanDT.Controllers
{
    public class ProductController : Controller
    {
        // GET: Product
        public ActionResult Product()
        {
            return View();
        }
        public ActionResult Details(int id)
        {
            using (var db = new WebBanDTEntities())
            {
                var product = db.Products
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
                        IsActive = p.IsActive
                    }).FirstOrDefault();

                if (product == null)
                {
                    return HttpNotFound();
                }

                return View(product);
            }
        }

    }
}