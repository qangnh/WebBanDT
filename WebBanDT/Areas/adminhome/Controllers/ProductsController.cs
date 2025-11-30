using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebBanDT.Models;
using PagedList;

namespace WebBanDT.Areas.AdminHome.Controllers
{
    public class ProductsController : Controller
    {
        private readonly WebBanDTEntities db = new WebBanDTEntities();

        // GET: AdminHome/Products
        public ActionResult Index(string searchString, int? page)
        {
            var products = db.Products
                             .Include(p => p.Category)
                             .Include(p => p.Brand)
                             .Include(p => p.ProductVersions)
                             .Include(p => p.ProductColors)
                             .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string keyword = searchString.Trim().ToLower();
                products = products.Where(p => p.ProductName.ToLower().Contains(keyword));
                ViewBag.SearchString = searchString;
            }

            // Lấy danh sách ProductID đã có trong OrderDetail (đã từng được mua)
            var purchasedProductIds = db.OrderDetails
                                        .Select(od => od.ProductID)
                                        .Distinct()
                                        .ToList();
            ViewBag.PurchasedProductIds = purchasedProductIds;

            products = products.OrderByDescending(p => p.CreatedAt);

            int pageSize = 5;
            int pageNumber = (page ?? 1);

            return View(products.ToPagedList(pageNumber, pageSize));
        }

        // GET: AdminHome/Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = db.Products
                            .Include(p => p.Category)
                            .Include(p => p.Brand)
                            .Include(p => p.ProductColors)
                            .Include(p => p.ProductVersions)
                            .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // GET: AdminHome/Products/Create
        public ActionResult Create()
        {
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName");
            // Brand để trống, sẽ load bằng Ajax sau khi chọn Category
            ViewBag.BrandID = new SelectList(Enumerable.Empty<Brand>(), "BrandID", "BrandName");
            return View();
        }

        // POST: AdminHome/Products/Create
        // POST: AdminHome/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include = "ProductID,CategoryID,BrandID,ProductName,ProductDescription,ProductPrice,StockQuantity,ProductImage,CreatedAt,IsActive")]
    Product product,
            string ColorInput,
            string VersionInput
        )
        {
            // ==== VALIDATE CreatedAt không được lớn hơn ngày hiện tại ====
            if (product.CreatedAt.HasValue && product.CreatedAt.Value.Date > DateTime.Now.Date)
            {
                ModelState.AddModelError("CreatedAt", "Ngày tạo không thể lớn hơn ngày hiện tại.");
            }

            if (ModelState.IsValid)
            {
                if (!product.CreatedAt.HasValue)
                    product.CreatedAt = DateTime.Now;

                db.Products.Add(product);
                db.SaveChanges(); // ProductID đã có

                // ================== LƯU MÀU SẮC ==================
                if (!string.IsNullOrWhiteSpace(ColorInput))
                {
                    var colorLines = ColorInput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in colorLines)
                    {
                        var raw = line.Trim();
                        if (string.IsNullOrEmpty(raw)) continue;

                        string colorName;
                        string colorImage = null;

                        var parts = raw.Split('|');
                        colorName = parts[0].Trim();

                        if (parts.Length > 1)
                            colorImage = parts[1].Trim();

                        if (!string.IsNullOrEmpty(colorName) && colorName.Length > 50)
                            colorName = colorName.Substring(0, 50);

                        if (!string.IsNullOrEmpty(colorImage) && colorImage.Length > 200)
                            colorImage = colorImage.Substring(0, 200);

                        if (!string.IsNullOrEmpty(colorName))
                        {
                            db.ProductColors.Add(new ProductColor
                            {
                                ProductID = product.ProductID,
                                ColorName = colorName,
                                ColorImage = colorImage
                            });
                        }
                    }
                }

                // ================== LƯU PHIÊN BẢN ==================
                if (!string.IsNullOrWhiteSpace(VersionInput))
                {
                    var versionLines = VersionInput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in versionLines)
                    {
                        var raw = line.Trim();
                        if (string.IsNullOrEmpty(raw)) continue;

                        string versionName;
                        decimal? versionPrice = null;

                        var parts = raw.Split('|');
                        versionName = parts[0].Trim();

                        if (parts.Length > 1 && decimal.TryParse(parts[1].Trim(), out decimal parsedPrice))
                            versionPrice = parsedPrice;

                        db.ProductVersions.Add(new ProductVersion
                        {
                            ProductID = product.ProductID,
                            VersionName = versionName,
                            VersionPrice = versionPrice
                        });
                    }
                }

                db.SaveChanges();

                return RedirectToAction("Index");
            }

            // Nếu ModelState lỗi → load lại dropdown
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);

            var brands = Enumerable.Empty<Brand>().AsQueryable();
            if (product.CategoryID != 0)
            {
                brands = db.Brands.Where(b => b.CategoryID == product.CategoryID && b.IsActive);
            }
            ViewBag.BrandID = new SelectList(brands.ToList(), "BrandID", "BrandName", product.BrandID);

            return View(product);
        }


        // GET: AdminHome/Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = db.Products
                            .Include(p => p.ProductVersions)
                            .Include(p => p.ProductColors)
                            .Include(p => p.Brand)
                            .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
                return HttpNotFound();

            // Prefill VersionInput
            ViewBag.VersionInput = string.Join("\r\n",
                product.ProductVersions.Select(v =>
                    v.VersionPrice.HasValue
                        ? $"{v.VersionName}|{v.VersionPrice.Value}"
                        : v.VersionName
                ));

            // Prefill ColorInput
            ViewBag.ColorInput = string.Join("\r\n",
                product.ProductColors.Select(c =>
                    string.IsNullOrEmpty(c.ColorImage)
                        ? c.ColorName
                        : $"{c.ColorName}|{c.ColorImage}"
                )
            );

            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);

            var brands = db.Brands
                           .Where(b => b.CategoryID == product.CategoryID && b.IsActive)
                           .OrderBy(b => b.BrandName)
                           .ToList();
            ViewBag.BrandID = new SelectList(brands, "BrandID", "BrandName", product.BrandID);

            return View(product);
        }

        // POST: AdminHome/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include = "ProductID,CategoryID,BrandID,ProductName,ProductDescription,ProductPrice,StockQuantity,ProductImage,CreatedAt,IsActive")]
    Product product,
            string VersionInput,
            string ColorInput
        )
        {
            // ==== VALIDATE CreatedAt không được lớn hơn ngày hiện tại ====
            if (product.CreatedAt.HasValue && product.CreatedAt.Value.Date > DateTime.Now.Date)
            {
                ModelState.AddModelError("CreatedAt", "Ngày tạo không thể lớn hơn ngày hiện tại.");
            }

            if (ModelState.IsValid)
            {
                var existing = db.Products
                                 .Include(p => p.ProductVersions)
                                 .Include(p => p.ProductColors)
                                 .FirstOrDefault(p => p.ProductID == product.ProductID);

                if (existing == null)
                    return HttpNotFound();

                // Cập nhật thông tin cơ bản
                existing.CategoryID = product.CategoryID;
                existing.BrandID = product.BrandID;
                existing.ProductName = product.ProductName;
                existing.ProductDescription = product.ProductDescription;
                existing.ProductPrice = product.ProductPrice;
                existing.StockQuantity = product.StockQuantity;
                existing.ProductImage = product.ProductImage;
                existing.CreatedAt = product.CreatedAt;
                existing.IsActive = product.IsActive;

                // XÓA & CẬP NHẬT LẠI PHIÊN BẢN
                db.ProductVersions.RemoveRange(existing.ProductVersions.ToList());

                if (!string.IsNullOrWhiteSpace(VersionInput))
                {
                    var versionLines = VersionInput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in versionLines)
                    {
                        var raw = line.Trim();
                        if (string.IsNullOrEmpty(raw)) continue;

                        string versionName = raw.Split('|')[0].Trim();
                        decimal? versionPrice = null;

                        if (raw.Split('|').Length > 1 &&
                            decimal.TryParse(raw.Split('|')[1].Trim(), out decimal parsedPrice))
                        {
                            versionPrice = parsedPrice;
                        }

                        db.ProductVersions.Add(new ProductVersion
                        {
                            ProductID = existing.ProductID,
                            VersionName = versionName,
                            VersionPrice = versionPrice
                        });
                    }
                }

                // XÓA & CẬP NHẬT LẠI MÀU SẮC
                db.ProductColors.RemoveRange(existing.ProductColors.ToList());

                if (!string.IsNullOrWhiteSpace(ColorInput))
                {
                    var colorLines = ColorInput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in colorLines)
                    {
                        var raw = line.Trim();
                        if (string.IsNullOrEmpty(raw)) continue;

                        string colorName = raw.Split('|')[0].Trim();
                        string colorImage = raw.Split('|').Length > 1 ? raw.Split('|')[1].Trim() : null;

                        if (!string.IsNullOrEmpty(colorName) && colorName.Length > 50)
                            colorName = colorName.Substring(0, 50);

                        if (!string.IsNullOrEmpty(colorImage) && colorImage.Length > 200)
                            colorImage = colorImage.Substring(0, 200);

                        db.ProductColors.Add(new ProductColor
                        {
                            ProductID = existing.ProductID,
                            ColorName = colorName,
                            ColorImage = colorImage
                        });
                    }
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu lỗi → load lại dropdown
            ViewBag.CategoryID = new SelectList(db.Categories, "CategoryID", "CategoryName", product.CategoryID);

            var brandsReload = db.Brands
                                 .Where(b => b.CategoryID == product.CategoryID && b.IsActive)
                                 .OrderBy(b => b.BrandName)
                                 .ToList();
            ViewBag.BrandID = new SelectList(brandsReload, "BrandID", "BrandName", product.BrandID);

            return View(product);
        }


        // GET: AdminHome/Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var product = db.Products
                            .Include(p => p.Category)
                            .Include(p => p.Brand)
                            .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // POST: AdminHome/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Kiểm tra xem sản phẩm đã từng được đặt trong đơn hàng chưa
            bool hasOrders = db.OrderDetails.Any(od => od.ProductID == id);

            if (hasOrders)
            {
                TempData["ErrorMessage"] = "Sản phẩm này đã được khách hàng mua, không thể xóa.";
                return RedirectToAction("Index");
            }

            // Nếu chưa từng nằm trong đơn hàng thì cho phép xóa như cũ
            var product = db.Products
                            .Include(p => p.ProductVersions)
                            .Include(p => p.ProductColors)
                            .FirstOrDefault(p => p.ProductID == id);

            if (product != null)
            {
                db.ProductVersions.RemoveRange(product.ProductVersions.ToList());
                db.ProductColors.RemoveRange(product.ProductColors.ToList());

                db.Products.Remove(product);
                db.SaveChanges();
            }

            TempData["SuccessMessage"] = "Xóa sản phẩm thành công.";
            return RedirectToAction("Index");
        }

        // JSON: Lấy Brand theo Category (cho Ajax)
        public JsonResult GetBrandsByCategory(int categoryId)
        {
            var brands = db.Brands
                           .Where(b => b.CategoryID == categoryId && b.IsActive)
                           .OrderBy(b => b.BrandName)
                           .Select(b => new
                           {
                               b.BrandID,
                               b.BrandName
                           })
                           .ToList();

            return Json(brands, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
