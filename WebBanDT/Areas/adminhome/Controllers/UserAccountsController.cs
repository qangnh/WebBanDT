using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebBanDT.Models;
using PagedList;

namespace WebBanDT.Areas.AdminHome.Controllers
{
    public class UserAccountsController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        // GET: AdminHome/UserAccounts
        public ActionResult Index(string searchString, int? page)
        {
            var users = db.UserAccounts.AsQueryable();

            // Tìm kiếm theo username hoặc email
            if (!string.IsNullOrEmpty(searchString))
            {
                string keyword = searchString.Trim().ToLower();
                users = users.Where(u => (u.Username ?? "").ToLower().Contains(keyword) ||
                                         (u.Email ?? "").ToLower().Contains(keyword));
                ViewBag.SearchString = searchString;
            }

            users = users.OrderBy(u => u.Username); // Sắp xếp theo Username

            int pageSize = 5;
            int pageNumber = (page ?? 1);

            db.Configuration.ProxyCreationEnabled = false;

            return View(users.ToPagedList(pageNumber, pageSize));
        }

        // GET: AdminHome/UserAccounts/Details/5
        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            UserAccount userAccount = db.UserAccounts.Find(id.Value);
            if (userAccount == null)
                return HttpNotFound();

            return View(userAccount);
        }

        // GET: AdminHome/UserAccounts/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdminHome/UserAccounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Username,PasswordHash,UserRole,CreatedAt,Email")] UserAccount userAccount)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    userAccount.PasswordHash = HashPassword(userAccount.PasswordHash);
                    userAccount.CreatedAt = DateTime.Now;

                    db.UserAccounts.Add(userAccount);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi tạo tài khoản: " + ex.Message);
                }
            }

            return View(userAccount);
        }

        // GET: AdminHome/UserAccounts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            UserAccount userAccount = db.UserAccounts.Find(id.Value);
            if (userAccount == null)
                return HttpNotFound();

            return View(userAccount);
        }

        // POST: AdminHome/UserAccounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserID,Username,PasswordHash,UserRole,CreatedAt,Email")] UserAccount userAccount)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = db.UserAccounts.Find(userAccount.UserID);
                    if (existingUser == null)
                        return HttpNotFound();

                    existingUser.UserRole = userAccount.UserRole;
                    existingUser.Email = userAccount.Email;

                    if (userAccount.CreatedAt != default(DateTime))
                        existingUser.CreatedAt = userAccount.CreatedAt;

                    if (!string.IsNullOrWhiteSpace(userAccount.PasswordHash))
                        existingUser.PasswordHash = HashPassword(userAccount.PasswordHash);

                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }

            return View(userAccount);
        }

        // GET: AdminHome/UserAccounts/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            UserAccount userAccount = db.UserAccounts.Find(id.Value);
            if (userAccount == null)
                return HttpNotFound();

            return View(userAccount);
        }

        // POST: AdminHome/UserAccounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            UserAccount userAccount = db.UserAccounts.Find(id);
            if (userAccount != null)
            {
                // Kiểm tra xem có Customer liên kết không
                bool hasCustomer = db.Customers.Any(c => c.UserID == id);
                if (hasCustomer)
                {
                    TempData["ErrorMessage"] = "User đang được liên kết với Customer, không thể xóa!";
                    return RedirectToAction("Index");
                }

                db.UserAccounts.Remove(userAccount);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        // GET: AdminHome/UserAccounts/UserProfile/5
        // GET: AdminHome/UserAccounts/UserProfile/5
      
    }
}
