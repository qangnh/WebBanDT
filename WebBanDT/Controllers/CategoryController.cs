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

        // 🔹 Hàm dùng chung để lấy sản phẩm theo danh mục + thương hiệu + giá
        private List<ProductVM> GetProductsByCategory(
            string categoryName,
            string keyword,
            decimal? minPrice,
            decimal? maxPrice,
            int? brandId
        )
        {
            var query = db.Products
                          .Where(p => p.Category.CategoryName == categoryName &&
                                      p.IsActive == true);

            // ⭐ Lọc theo thương hiệu (BrandID)
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandID == brandId.Value);
            }

            // Tìm kiếm theo từ khóa trong tên / mô tả
            if (!string.IsNullOrEmpty(keyword))
            {
                string lowerKeyword = keyword.ToLower();
                query = query.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Contains(lowerKeyword)) ||
                    (p.ProductDescription != null && p.ProductDescription.ToLower().Contains(lowerKeyword))
                );
            }

            // Lọc theo giá từ
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.ProductPrice >= minPrice.Value);
            }

            // Lọc theo giá đến
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
                    // Nếu ProductVM có BrandName thì thêm:
                    // BrandName = p.Brand.BrandName
                })
                .ToList();
        }

        // 🔹 Hàm dùng chung đổ Category + Brand xuống ViewBag
        private void LoadCategoryInfoToViewBag(string categoryName)
        {
            var category = db.Categories.FirstOrDefault(c => c.CategoryName == categoryName);
            if (category != null)
            {
                ViewBag.CategoryId = category.CategoryID;
                ViewBag.CategoryName = category.CategoryName;

                var brands = db.Brands
                               .Where(b => b.CategoryID == category.CategoryID && b.IsActive)
                               .OrderBy(b => b.BrandName)
                               .ToList();
                ViewBag.Brands = brands;
            }
        }

        // 🔹 Điện thoại
        public ActionResult Phones(string keyword, decimal? minPrice, decimal? maxPrice, int? brandId)
        {
            var products = GetProductsByCategory("Điện thoại", keyword, minPrice, maxPrice, brandId);
            LoadCategoryInfoToViewBag("Điện thoại");
            return View(products);
        }

        // 🔹 Laptop
        public ActionResult Laptop(string keyword, decimal? minPrice, decimal? maxPrice, int? brandId)
        {
            var products = GetProductsByCategory("Laptop", keyword, minPrice, maxPrice, brandId);
            LoadCategoryInfoToViewBag("Laptop");
            return View(products);
        }

        // 🔹 Màn hình
        public ActionResult Screen(string keyword, decimal? minPrice, decimal? maxPrice, int? brandId)
        {
            var products = GetProductsByCategory("Màn hình", keyword, minPrice, maxPrice, brandId);
            LoadCategoryInfoToViewBag("Màn hình");
            return View(products);
        }

        // 🔹 Tablet
        public ActionResult Tablet(string keyword, decimal? minPrice, decimal? maxPrice, int? brandId)
        {
            var products = GetProductsByCategory("Tablet", keyword, minPrice, maxPrice, brandId);
            LoadCategoryInfoToViewBag("Tablet");
            return View(products);
        }

        // 🔹 Âm thanh
        public ActionResult Sound(string keyword, decimal? minPrice, decimal? maxPrice, int? brandId)
        {
            var products = GetProductsByCategory("Âm thanh", keyword, minPrice, maxPrice, brandId);
            LoadCategoryInfoToViewBag("Âm thanh");
            return View(products);
        }

        // 🔹 Đồng hồ
        public ActionResult Watch(string keyword, decimal? minPrice, decimal? maxPrice, int? brandId)
        {
            var products = GetProductsByCategory("Đồng hồ", keyword, minPrice, maxPrice, brandId);
            LoadCategoryInfoToViewBag("Đồng hồ");
            return View(products);
        }

        // 🔹 Tất cả sản phẩm (không lọc theo category, bạn có thể thêm brandId nếu thích)
        public ActionResult FullProduct(string keyword, decimal? minPrice, decimal? maxPrice)
        {
            // Lấy tất cả sản phẩm đang active, không lọc theo category
            var query = db.Products.Where(p => p.IsActive == true);

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                string lowerKeyword = keyword.ToLower();
                query = query.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Contains(lowerKeyword)) ||
                    (p.ProductDescription != null && p.ProductDescription.ToLower().Contains(lowerKeyword))
                );
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
                .Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Contains(lowerKeyword)) ||
                    (p.ProductDescription != null && p.ProductDescription.ToLower().Contains(lowerKeyword))
                );

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
                .Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Contains(lowerKeyword)) ||
                    (p.ProductDescription != null && p.ProductDescription.ToLower().Contains(lowerKeyword))
                )
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
