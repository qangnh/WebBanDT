using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebBanDT.Models;

namespace WebBanDT.Areas.AdminHome.Controllers
{
    public class OrdersController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        // GET: AdminHome/Orders
        public ActionResult Index(string searchString, int? page)
        {
            var orders = db.Orders.Include(o => o.Customer).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string keyword = searchString.Trim().ToLower();
                orders = orders.Where(o => o.OrderID.ToString().Contains(keyword)
                                         || o.Customer.CustomerName.ToLower().Contains(keyword));
                ViewBag.SearchString = searchString;
            }

            orders = orders.OrderByDescending(o => o.OrderDate); // dùng đúng tên thuộc tính ngày đặt

            int pageSize = 5;
            int pageNumber = (page ?? 1);

            db.Configuration.ProxyCreationEnabled = false;

            return View(orders.ToPagedList(pageNumber, pageSize));
        }

        // GET: AdminHome/Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Order order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // GET: AdminHome/Orders/Create
        public ActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: AdminHome/Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OrderID,CustomerID,OrderDate,TotalAmount,PaymentStatus,DeliveryAddress,OrderStatus")] Order order)
        {
            if (order.OrderDate == null)
            {
                order.OrderDate = DateTime.Now;
            }

            if (ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            PopulateDropdowns(order);
            return View(order);
        }

        // GET: AdminHome/Orders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Order order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            PopulateDropdowns(order);
            return View(order);
        }

        // POST: AdminHome/Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderID,CustomerID,OrderDate,TotalAmount,PaymentStatus,DeliveryAddress,OrderStatus")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            PopulateDropdowns(order);
            return View(order);
        }

        // GET: AdminHome/Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Order order = db.Orders.Find(id);
            if (order == null)
                return HttpNotFound();

            return View(order);
        }

        // POST: AdminHome/Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.Orders.Find(id);
            db.Orders.Remove(order);
            db.SaveChanges();
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

        // ✅ Gán dropdown cho Customer, PaymentStatus, OrderStatus
        private void PopulateDropdowns(Order order = null)
        {
            ViewBag.CustomerID = new SelectList(db.Customers, "CustomerID", "CustomerName", order?.CustomerID);

            ViewBag.PaymentStatusList = new SelectList(new[]
            {
                new { Value = "Đã thanh toán", Text = "Đã thanh toán" },
                new { Value = "Chưa thanh toán", Text = "Chưa thanh toán" }
            }, "Value", "Text", order?.PaymentStatus);

            ViewBag.OrderStatusList = new SelectList(new[]
            {
                new { Value = "Đang xử lý", Text = "Đang xử lý" },
                new { Value = "Đã giao", Text = "Đã giao" },
                new { Value = "Hủy", Text = "Hủy" }
            }, "Value", "Text", order?.OrderStatus);
        }
    }
}
