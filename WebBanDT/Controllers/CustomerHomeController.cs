using System;
using System.Linq;
using System.Web.Mvc;
using WebBanDT.Models;
using WebBanDT.Models.ViewModels;

namespace WebBanDT.Controllers
{
    public class CustomerHomeController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        public ActionResult Index()
        {
            // Lấy dữ liệu sản phẩm cho các category
            var vm = new CustomerHomeVM
            {
                Phones = db.Products
                    .Where(p => p.IsActive == true && p.Category.CategoryName.Contains("Điện thoại"))
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new ProductVM
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        ProductPrice = p.ProductPrice,
                        StockQuantity = p.StockQuantity,
                        ProductImage = p.ProductImage,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.IsActive,
                        CategoryName = p.Category.CategoryName
                    })
                    .Take(5)
                    .ToList(),

                Laptops = db.Products
                    .Where(p => p.IsActive == true && p.Category.CategoryName.Contains("Laptop"))
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new ProductVM
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        ProductPrice = p.ProductPrice,
                        StockQuantity = p.StockQuantity,
                        ProductImage = p.ProductImage,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.IsActive,
                        CategoryName = p.Category.CategoryName
                    })
                    .Take(5)
                    .ToList(),

                Tablets = db.Products
                    .Where(p => p.IsActive == true && p.Category.CategoryName.Contains("Tablet"))
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new ProductVM
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        ProductPrice = p.ProductPrice,
                        StockQuantity = p.StockQuantity,
                        ProductImage = p.ProductImage,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.IsActive,
                        CategoryName = p.Category.CategoryName
                    })
                    .Take(5)
                    .ToList(),

                Watches = db.Products
                    .Where(p => p.IsActive == true && p.Category.CategoryName.Contains("Đồng hồ"))
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new ProductVM
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        ProductPrice = p.ProductPrice,
                        StockQuantity = p.StockQuantity,
                        ProductImage = p.ProductImage,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.IsActive,
                        CategoryName = p.Category.CategoryName
                    })
                    .Take(5)
                    .ToList(),

                Sounds = db.Products
                    .Where(p => p.IsActive == true && p.Category.CategoryName.Contains("Âm thanh"))
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new ProductVM
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        ProductPrice = p.ProductPrice,
                        StockQuantity = p.StockQuantity,
                        ProductImage = p.ProductImage,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.IsActive,
                        CategoryName = p.Category.CategoryName
                    })
                    .Take(5)
                    .ToList(),

                Screens = db.Products
                    .Where(p => p.IsActive == true && p.Category.CategoryName.Contains("Màn hình"))
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new ProductVM
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        ProductDescription = p.ProductDescription,
                        ProductPrice = p.ProductPrice,
                        StockQuantity = p.StockQuantity,
                        ProductImage = p.ProductImage,
                        CreatedAt = p.CreatedAt,
                        IsActive = p.IsActive,
                        CategoryName = p.Category.CategoryName
                    })
                    .Take(5)
                    .ToList()
            };

            // --- TÍNH cartCount: TỔNG SỐ LƯỢNG SẢN PHẨM TRONG GIỎ ---
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

            return View(vm);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Không kiểm tra Session cho trang chủ
            base.OnActionExecuting(filterContext);
        }
    }
}
