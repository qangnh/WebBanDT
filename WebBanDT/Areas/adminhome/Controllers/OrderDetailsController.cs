using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using WebBanDT.Models;
using PagedList;

namespace WebBanDT.Areas.AdminHome.Controllers
{
    public class OrderDetailsController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        // GET: AdminHome/OrderDetails
        public ActionResult Index(string searchString, int? page)
        {
            var orderDetails = db.OrderDetails
                                 .Include(od => od.Order)
                                 .Include(od => od.Product)
                                 .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string keyword = searchString.Trim().ToLower();
                orderDetails = orderDetails.Where(od => od.Product.ProductName.ToLower().Contains(keyword));
                ViewBag.SearchString = searchString;
            }

            orderDetails = orderDetails.OrderByDescending(od => od.OrderDetailID);

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(orderDetails.ToPagedList(pageNumber, pageSize));
        }

        // GET: AdminHome/OrderDetails/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var orderDetail = db.OrderDetails
                                .Include(od => od.Order)
                                .Include(od => od.Product)
                                .FirstOrDefault(od => od.OrderDetailID == id);

            if (orderDetail == null)
                return HttpNotFound();

            return View(orderDetail);
        }

        // GET: AdminHome/OrderDetails/Create
        public ActionResult Create()
        {
            ViewBag.OrderID = new SelectList(db.Orders, "OrderID", "PaymentStatus");
            ViewBag.ProductID = new SelectList(db.Products, "ProductID", "ProductName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OrderDetailID,OrderID,ProductID,Quantity,UnitPrice")] OrderDetail orderDetail)
        {
            if (ModelState.IsValid)
            {
                db.OrderDetails.Add(orderDetail);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.OrderID = new SelectList(db.Orders, "OrderID", "PaymentStatus", orderDetail.OrderID);
            ViewBag.ProductID = new SelectList(db.Products, "ProductID", "ProductName", orderDetail.ProductID);
            return View(orderDetail);
        }

        // GET: AdminHome/OrderDetails/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var orderDetail = db.OrderDetails
                                .Include(od => od.Order)
                                .Include(od => od.Product)
                                .FirstOrDefault(od => od.OrderDetailID == id);

            if (orderDetail == null)
                return HttpNotFound();

            ViewBag.OrderID = new SelectList(db.Orders, "OrderID", "PaymentStatus", orderDetail.OrderID);
            ViewBag.ProductID = new SelectList(db.Products, "ProductID", "ProductName", orderDetail.ProductID);
            return View(orderDetail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderDetailID,OrderID,ProductID,Quantity,UnitPrice")] OrderDetail orderDetail)
        {
            if (ModelState.IsValid)
            {
                db.Entry(orderDetail).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.OrderID = new SelectList(db.Orders, "OrderID", "PaymentStatus", orderDetail.OrderID);
            ViewBag.ProductID = new SelectList(db.Products, "ProductID", "ProductName", orderDetail.ProductID);
            return View(orderDetail);
        }

        // GET: AdminHome/OrderDetails/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var orderDetail = db.OrderDetails
                                .Include(od => od.Order)
                                .Include(od => od.Product)
                                .FirstOrDefault(od => od.OrderDetailID == id);

            if (orderDetail == null)
                return HttpNotFound();

            return View(orderDetail);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var orderDetail = db.OrderDetails.Find(id);
            db.OrderDetails.Remove(orderDetail);
            db.SaveChanges();
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
