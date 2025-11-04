using System;
using System.Linq;
using System.Web.Mvc;
using WebBanDT.Models;

namespace WebBanDT.Areas.AdminHome.Controllers
{
    public class HomeController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        public ActionResult Index()
        {
            try
            {
                // Lấy năm hiện tại
                int currentYear = DateTime.Now.Year;

                // === DOANH SỐ THEO THÁNG ===
                var monthlySales = db.OrderDetails
                    .Where(od => od.Order.OrderDate.HasValue
                              && od.Order.OrderDate.Value.Year == currentYear
                              && od.Order.OrderStatus == "Đã giao") // Chỉ tính đơn đã giao
                    .GroupBy(od => od.Order.OrderDate.Value.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Total = g.Sum(x => (decimal?)(x.Quantity * x.UnitPrice)) ?? 0
                    })
                    .OrderBy(x => x.Month)
                    .ToList();

                // Tạo đầy đủ 12 tháng (bao gồm cả tháng không có dữ liệu)
                var allMonths = Enumerable.Range(1, 12)
                    .Select(month => new
                    {
                        Month = month,
                        Total = monthlySales.FirstOrDefault(m => m.Month == month)?.Total ?? 0
                    })
                    .ToList();

                ViewBag.MonthLabels = allMonths.Select(x => "Tháng " + x.Month).ToArray();
                ViewBag.MonthTotals = allMonths.Select(x => x.Total).ToArray();

                // === DOANH SỐ THEO DANH MỤC ===
                var categorySales = db.OrderDetails
                    .Where(od => od.Order.OrderDate.HasValue
                              && od.Order.OrderDate.Value.Year == currentYear
                              && od.Order.OrderStatus == "Đã giao") // Chỉ tính đơn đã giao
                    .GroupBy(od => od.Product.Category.CategoryName)
                    .Select(g => new
                    {
                        Category = g.Key ?? "Khác",
                        Total = g.Sum(x => (decimal?)(x.Quantity * x.UnitPrice)) ?? 0
                    })
                    .Where(x => x.Total > 0) // Chỉ lấy danh mục có doanh số
                    .OrderByDescending(x => x.Total) // Sắp xếp từ cao đến thấp
                    .ToList();

                ViewBag.CategoryLabels = categorySales.Select(x => x.Category).ToArray();
                ViewBag.CategoryTotals = categorySales.Select(x => x.Total).ToArray();

                // === THỐNG KÊ THÊM (optional) ===
                ViewBag.TotalRevenue = allMonths.Sum(x => x.Total);
                ViewBag.TotalOrders = db.Orders
                    .Count(o => o.OrderDate.HasValue
                             && o.OrderDate.Value.Year == currentYear
                             && o.OrderStatus == "Đã giao");
                ViewBag.CurrentYear = currentYear;

                return View();
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu: " + ex.Message;

                // Trả về dữ liệu mặc định
                ViewBag.MonthLabels = Enumerable.Range(1, 12).Select(m => "Tháng " + m).ToArray();
                ViewBag.MonthTotals = new decimal[12];
                ViewBag.CategoryLabels = new string[] { };
                ViewBag.CategoryTotals = new decimal[] { };

                return View();
            }
        }

        // Giải phóng DbContext khi controller bị dispose
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}