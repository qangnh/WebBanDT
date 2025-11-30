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

            // 🔹 TÍNH DANH SÁCH USER KHÔNG ĐƯỢC PHÉP XÓA
            // Giả sử bảng Customer có cột UserID và Order có CustomerID (đúng với pattern bạn đang dùng)
            var userCannotDelete = db.Orders
                                     .Select(o => o.CustomerID)
                                     .Distinct()
                                     .Join(db.Customers,
                                           orderCustomerId => orderCustomerId,
                                           c => c.CustomerID,
                                           (orderCustomerId, c) => c.UserID)
                                     .Distinct()
                                     .ToList();

            ViewBag.UserCannotDelete = userCannotDelete;

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

            var userAccount = db.UserAccounts.Find(id.Value);
            if (userAccount == null)
                return HttpNotFound();

            // Tìm customer liên kết với user này
            var customers = db.Customers
                              .Where(c => c.UserID == id.Value)
                              .ToList();

            bool hasOrders = false;
            if (customers.Any())
            {
                var customerIds = customers.Select(c => c.CustomerID).ToList();
                hasOrders = db.Orders.Any(o => customerIds.Contains(o.CustomerID));
            }

            // ❗ Không được xóa admin, hoặc user đang có đơn hàng
            bool isAdmin = string.Equals(userAccount.UserRole, "Admin", StringComparison.OrdinalIgnoreCase);
            ViewBag.CanDelete = !(hasOrders || isAdmin);

            return View(userAccount);
        }



        // POST: AdminHome/UserAccounts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var userAccount = db.UserAccounts.Find(id);
            if (userAccount == null)
                return HttpNotFound();

            // ❗ Chặn xóa tài khoản Admin
            if (string.Equals(userAccount.UserRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản Admin.";
                return RedirectToAction("Index");
            }

            // Tìm customer gắn với user này
            var customers = db.Customers
                              .Where(c => c.UserID == id)
                              .ToList();

            // Kiểm tra có đơn hàng không
            bool hasOrders = false;
            if (customers.Any())
            {
                var customerIds = customers.Select(c => c.CustomerID).ToList();
                hasOrders = db.Orders.Any(o => customerIds.Contains(o.CustomerID));
            }

            if (hasOrders)
            {
                TempData["ErrorMessage"] = "Tài khoản này đang được sử dụng bởi khách hàng đã phát sinh đơn hàng, không thể xóa.";
                return RedirectToAction("Index");
            }

            // 1️⃣ Xóa Cart
            var carts = db.Carts.Where(c => c.UserID == id).ToList();
            if (carts.Any())
            {
                db.Carts.RemoveRange(carts);
            }

            // 2️⃣ Xóa Customers (chỉ khi chưa có đơn hàng)
            if (customers.Any())
            {
                db.Customers.RemoveRange(customers);
            }

            // 3️⃣ Xóa User
            db.UserAccounts.Remove(userAccount);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Xóa tài khoản thành công!";
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
      
    }
}
