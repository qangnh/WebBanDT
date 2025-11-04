using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebBanDT.Models;

namespace WebBanDT.Controllers
{
    public class AccountController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        // =================== REGISTER ===================
        public ActionResult Register(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string FullName, string Phone, string Address, string Email, string Password, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            // --- Kiểm tra dữ liệu trống ---
            if (string.IsNullOrWhiteSpace(FullName) ||
                string.IsNullOrWhiteSpace(Phone) ||
                string.IsNullOrWhiteSpace(Address) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin!");
                return View();
            }

            // --- Kiểm tra email trùng ---
            if (db.UserAccounts.Any(u => u.Email == Email))
            {
                ModelState.AddModelError("", "Email đã được sử dụng!");
                return View();
            }

            // --- Tạo tài khoản ---
            var user = new UserAccount
            {
                Username = Email,
                Email = Email,
                PasswordHash = HashPassword(Password),
                UserRole = "User",
                CreatedAt = DateTime.Now
            };
            db.UserAccounts.Add(user);
            db.SaveChanges(); // Lưu để có UserID

            // --- Tạo khách hàng ---
            var customer = new Customer
            {
                CustomerName = FullName,
                CustomerPhone = Phone,
                CustomerAddress = Address,
                CustomerEmail = Email,
                CreatedAt = DateTime.Now,
                Username = Email,
                UserID = user.UserID // Gắn liên kết
            };
            db.Customers.Add(customer);
            db.SaveChanges();

            // --- Gán session ---
            Session["UserID"] = user.UserID;
            Session["Username"] = user.Username;
            Session["AvatarPath"] = "/Image/icon.jpg";
            Session["UserRole"] = user.UserRole;
            Session["CustomerID"] = customer.CustomerID;

            // --- Điều hướng ---
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "CustomerHome");


        }

        // =================== LOGIN ===================
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string Username, string Password, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            // --- Kiểm tra dữ liệu ---
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.Error = "Vui lòng nhập tên đăng nhập và mật khẩu!";
                return View();
            }

            string passwordHash = HashPassword(Password);

            var user = db.UserAccounts.FirstOrDefault(u =>
                (u.Username == Username || u.Email == Username) &&
                u.PasswordHash == passwordHash);

            if (user == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View();
            }

            // --- Lấy thông tin khách hàng ---
            var customer = db.Customers.FirstOrDefault(c => c.UserID == user.UserID || c.Username == user.Username);

            // --- Gán session ---
            Session["UserID"] = user.UserID;
            Session["Username"] = user.Username;
            Session["AvatarPath"] = string.IsNullOrEmpty(user.AvatarPath) ? "/Image/icon.jpg" : user.AvatarPath;
            Session["UserRole"] = user.UserRole;
            Session["CustomerID"] = customer?.CustomerID;

            // --- Điều hướng ---
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            if (user.UserRole == "Admin")
                return RedirectToAction("Index", "UserAccounts", new { area = "AdminHome" });

            return RedirectToAction("Index", "CustomerHome");
        }

        // =================== LOGOUT ===================
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        // =================== HASH PASSWORD ===================
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
