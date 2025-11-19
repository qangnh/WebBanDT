using PagedList;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebBanDT.Models;
using WebBanDT.Models.viewmodels;

namespace WebBanDT.Areas.AdminHome.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly WebBanDTEntities db = new WebBanDTEntities();

        // GET: AdminHome/Categories
        public ActionResult Index(string searchString, int? page)
        {
            var categories = db.Categories
                               .Include(c => c.Brands)   // 👈 thêm dòng này
                               .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string keyword = searchString.Trim().ToLower();
                categories = categories.Where(c => c.CategoryName.ToLower().Contains(keyword));
                ViewBag.SearchString = searchString;
            }

            categories = categories.OrderBy(c => c.CategoryName);

            int pageSize = 5;
            int pageNumber = (page ?? 1);

            return View(categories.ToPagedList(pageNumber, pageSize));
        }


        // GET: AdminHome/Categories/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var category = db.Categories.Find(id);
            if (category == null) return HttpNotFound();

            return View(category);
        }

        // ================== CREATE ==================

        // GET: AdminHome/Categories/Create
        public ActionResult Create()
        {
            var vm = new CategoryWithBrandsViewModel
            {
                Category = new Category()
            };
            return View(vm);
        }

        // POST: AdminHome/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CategoryWithBrandsViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Lưu Category
            model.Category.CreatedAt = DateTime.Now;
            db.Categories.Add(model.Category);
            db.SaveChanges(); // Có CategoryID

            // Lưu danh sách Brand nếu có
            if (!string.IsNullOrWhiteSpace(model.BrandNames))
            {
                var names = model.BrandNames
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim())
                    .Where(n => n.Length > 0)
                    .Distinct();

                foreach (var name in names)
                {
                    var brand = new Brand
                    {
                        BrandName = name,
                        CategoryID = model.Category.CategoryID,
                        IsActive = true
                    };
                    db.Brands.Add(brand);
                }

                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // ================== EDIT ==================

        // GET: AdminHome/Categories/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var category = db.Categories.Find(id);
            if (category == null) return HttpNotFound();

            // Lấy danh sách brand hiện tại của Category
            var brands = db.Brands
                           .Where(b => b.CategoryID == category.CategoryID)
                           .OrderBy(b => b.BrandName)
                           .Select(b => b.BrandName)
                           .ToList();

            var vm = new CategoryWithBrandsViewModel
            {
                Category = category,
                // ví dụ: "iPhone, Samsung, Xiaomi"
                BrandNames = string.Join(", ", brands)
            };

            return View(vm);
        }

        // POST: AdminHome/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CategoryWithBrandsViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Cập nhật Category
            db.Entry(model.Category).State = EntityState.Modified;

            // Xóa các Brand cũ thuộc Category này
            var oldBrands = db.Brands.Where(b => b.CategoryID == model.Category.CategoryID);
            db.Brands.RemoveRange(oldBrands);

            // Thêm lại các Brand mới nhập
            if (!string.IsNullOrWhiteSpace(model.BrandNames))
            {
                var names = model.BrandNames
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim())
                    .Where(n => n.Length > 0)
                    .Distinct();

                foreach (var name in names)
                {
                    var brand = new Brand
                    {
                        BrandName = name,
                        CategoryID = model.Category.CategoryID,
                        IsActive = true
                    };
                    db.Brands.Add(brand);
                }
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ================== DELETE ==================

        // GET: AdminHome/Categories/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var category = db.Categories.Find(id);
            if (category == null) return HttpNotFound();

            return View(category);
        }

        // POST: AdminHome/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var category = db.Categories.Find(id);
            if (category != null)
            {
                // (option) có thể xóa luôn brand thuộc category này nếu cần
                var brands = db.Brands.Where(b => b.CategoryID == category.CategoryID);
                db.Brands.RemoveRange(brands);

                db.Categories.Remove(category);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
