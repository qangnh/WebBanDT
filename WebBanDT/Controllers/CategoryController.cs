using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebBanDT.Models;
using WebBanDT.Models.ViewModels;

namespace WebBanDT.Controllers
{
    public class CategoryController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        // 🔹 Hàm dùng chung để lấy sản phẩm theo danh mục
        private List<ProductVM> GetProductsByCategory(string categoryName, string keyword, decimal? minPrice, decimal? maxPrice)
        {
            var query = db.Products
                .Where(p => p.Category.CategoryName == categoryName && p.IsActive == true);

            if (!string.IsNullOrEmpty(keyword))
            {
                string lowerKeyword = keyword.ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(lowerKeyword) ||
                                         p.ProductDescription.ToLower().Contains(lowerKeyword));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.ProductPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.ProductPrice <= maxPrice.Value);
            }

            return query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductVM
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    ProductPrice = p.ProductPrice,
                    ProductImage = p.ProductImage,
                    ProductDescription = p.ProductDescription,
                    CategoryName = p.Category.CategoryName,
                    StockQuantity = p.StockQuantity,
                    CreatedAt = p.CreatedAt,
                    IsActive = p.IsActive
                })
                .ToList();
        }

        // 🔹 Các action danh mục
        public ActionResult Phones(string keyword, decimal? minPrice, decimal? maxPrice)
        {
            var products = GetProductsByCategory("Điện thoại", keyword, minPrice, maxPrice);
            return View(products);
        }

        public ActionResult Laptop(string keyword, decimal? minPrice, decimal? maxPrice)
        {
            var products = GetProductsByCategory("Laptop", keyword, minPrice, maxPrice);
            return View(products);
        }

        public ActionResult Screen(string keyword, decimal? minPrice, decimal? maxPrice)
        {
            var products = GetProductsByCategory("Màn hình", keyword, minPrice, maxPrice);
            return View(products);
        }

        public ActionResult Tablet(string keyword, decimal? minPrice, decimal? maxPrice)
        {
            var products = GetProductsByCategory("Tablet", keyword, minPrice, maxPrice);
            return View(products);
        }

        public ActionResult Sound(string keyword, decimal? minPrice, decimal? maxPrice)
        {
            var products = GetProductsByCategory("Âm thanh", keyword, minPrice, maxPrice);
            return View(products);
        }

        public ActionResult Watch(string keyword, decimal? minPrice, decimal? maxPrice)
        {
            var products = GetProductsByCategory("Đồng hồ", keyword, minPrice, maxPrice);
            return View(products);
        }
        public ActionResult FullProduct(string keyword, decimal? minPrice, decimal? maxPrice)
        {
            // Lấy tất cả sản phẩm đang active, không lọc theo category
            var query = db.Products.Where(p => p.IsActive == true);

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                string lowerKeyword = keyword.ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(lowerKeyword) ||
                                         p.ProductDescription.ToLower().Contains(lowerKeyword));
            }

            // Lọc theo giá tối thiểu
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.ProductPrice >= minPrice.Value);
            }

            // Lọc theo giá tối đa
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.ProductPrice <= maxPrice.Value);
            }

            // Lấy danh sách sản phẩm và sắp xếp
            var products = query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductVM
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    ProductPrice = p.ProductPrice,
                    ProductImage = p.ProductImage,
                    ProductDescription = p.ProductDescription,
                    CategoryName = p.Category.CategoryName,
                    StockQuantity = p.StockQuantity,
                    CreatedAt = p.CreatedAt,
                    IsActive = p.IsActive
                })
                .ToList();

            return View(products);
        }

        // 🔹 Tìm kiếm tất cả sản phẩm
        public ActionResult SearchAll(string keyword, decimal? minPrice, decimal? maxPrice)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return RedirectToAction("Index", "CustomerHome");
            }

            ViewBag.SearchKeyword = keyword;
            string lowerKeyword = keyword.ToLower();

            var query = db.Products
                .Where(p => p.IsActive == true)
                .Where(p => p.ProductName.ToLower().Contains(lowerKeyword) ||
                            p.ProductDescription.ToLower().Contains(lowerKeyword));

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.ProductPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.ProductPrice <= maxPrice.Value);
            }

            var products = query
                .OrderBy(p => p.ProductName)
                .Select(p => new ProductVM
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    ProductPrice = p.ProductPrice,
                    ProductImage = p.ProductImage,
                    ProductDescription = p.ProductDescription,
                    CategoryName = p.Category.CategoryName
                })
                .ToList();

            ViewBag.Title = $"Kết quả tìm kiếm: {keyword}";
            ViewBag.ResultCount = products.Count;

            return View(products);
        }

        // 🔹 Tìm kiếm gợi ý AJAX
        [HttpGet]
        public JsonResult SearchSuggestions(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            string lowerKeyword = keyword.ToLower();

            var suggestions = db.Products
                .Where(p => p.IsActive == true)
                .Where(p => p.ProductName.ToLower().Contains(lowerKeyword) ||
                            p.ProductDescription.ToLower().Contains(lowerKeyword))
                .Take(8)
                .Select(p => new
                {
                    id = p.ProductID,
                    name = p.ProductName,
                    price = p.ProductPrice,
                    image = p.ProductImage,
                    category = p.Category.CategoryName
                })
                .ToList();

            return Json(new { success = true, data = suggestions }, JsonRequestBehavior.AllowGet);
        }
    }
}
