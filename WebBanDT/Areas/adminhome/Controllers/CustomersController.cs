using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using WebBanDT.Models;
using BCrypt.Net;


namespace WebBanDT.Areas.AdminHome.Controllers
{
    public class CustomersController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        // GET: AdminHome/Customers
        public ActionResult Index(string searchString, int? page)
        {
            IQueryable<Customer> customers = db.Customers.Include(c => c.UserAccount);

            if (!String.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c =>
                    c.CustomerName.Contains(searchString) ||
                    c.CustomerPhone.Contains(searchString) ||
                    c.CustomerEmail.Contains(searchString)
                );
            }

            customers = customers.OrderBy(c => c.CustomerName);

            int pageSize = 5;
            int pageNumber = (page ?? 1);

            return View(customers.ToPagedList(pageNumber, pageSize));
        }

        // GET: AdminHome/Customers/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var customer = db.Customers.Find(id);
            if (customer == null) return HttpNotFound();

            return View(customer);
        }

        // GET: AdminHome/Customers/Create
        public ActionResult Create()
        {
            PopulateUserDropdown();
            return View();
        }

        // POST: AdminHome/Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CustomerID,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress,Username,CreatedAt")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra username có tồn tại chưa
                var existingUser = db.UserAccounts.FirstOrDefault(u => u.Username == customer.Username);

                if (existingUser == null)
                {
                    // Nếu chưa có user, tạo mới
                    var newUser = new UserAccount
                    {
                        Username = customer.Username,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), // Mật khẩu mặc định, có thể thay đổi
                        UserRole = "Customer",
                        Email = customer.CustomerEmail,
                        CreatedAt = DateTime.Now
                    };

                    db.UserAccounts.Add(newUser);
                }

                // Gán thời gian tạo cho customer
                customer.CreatedAt = DateTime.Now;

                // Thêm khách hàng
                db.Customers.Add(customer);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            PopulateUserDropdown(customer.Username);
            return View(customer);
        }

        // GET: AdminHome/Customers/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var customer = db.Customers.Find(id);
            if (customer == null) return HttpNotFound();

            PopulateUserDropdown(customer.Username);
            return View(customer);
        }

        // POST: AdminHome/Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CustomerID,CustomerName,CustomerPhone,CustomerEmail,CustomerAddress,Username,CreatedAt")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customer).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            PopulateUserDropdown(customer.Username);
            return View(customer);
        }

        // GET: AdminHome/Customers/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var customer = db.Customers.Find(id);
            if (customer == null) return HttpNotFound();

            return View(customer);
        }

        // POST: AdminHome/Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var customer = db.Customers.Find(id);
            db.Customers.Remove(customer);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // Dropdown helper
        private void PopulateUserDropdown(string selectedUsername = null)
        {
            var users = db.UserAccounts
                          .Select(u => new SelectListItem
                          {
                              Value = u.Username,
                              Text = u.Username,
                              Selected = u.Username == selectedUsername
                          }).ToList();

            ViewBag.UserList = users;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
