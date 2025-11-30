using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using WebBanDT.Models;
using WebBanDT.Models.viewmodels;

namespace WebBanDT.Controllers
{
    public class UsersController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        // Helper: Set KPI data cho tất cả views
        private void SetKPIData()
        {
            if (Session["UserID"] == null)
                return;

            int userId = (int)Session["UserID"];
            var user = db.UserAccounts.Find(userId);
            var customer = db.Customers.FirstOrDefault(c => c.UserID == userId);

            ViewBag.CustomerName = customer?.CustomerName ?? user?.Username ?? "";
            ViewBag.Phone = MaskPhone(customer?.CustomerPhone ?? "");
            ViewBag.Email = customer?.CustomerEmail ?? user?.Email ?? "";
            ViewBag.UpdateDate = "01/01/2026";

            // Tính tổng đơn hàng
            ViewBag.TotalOrders = customer?.Orders.Count() ?? 0;

            // Tính tổng tiền tích lũy (chỉ đơn đã giao)
            var totalSpent = customer?.Orders
                .Where(o => o.OrderStatus == "Đã giao hàng")
                .Sum(o => o.TotalAmount) ?? 0;
            ViewBag.TotalSpent = totalSpent.ToString("N0") + "đ";
        }

        private string MaskPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 5)
                return phone;
            return phone.Substring(0, 3) + "****" + phone.Substring(phone.Length - 2);
        }

        // ==========================================
        // GET: /Users/Profile - TỔNG QUAN
        // ==========================================
        public ActionResult Profile()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            SetKPIData();

            int userId = (int)Session["UserID"];
            var user = db.UserAccounts.Find(userId);
            var customer = db.Customers.FirstOrDefault(c => c.UserID == userId);

            var model = new ProfileOverviewViewModel
            {
                UserID = user.UserID,
                FullName = customer?.CustomerName ?? user.Username,
                Phone = customer?.CustomerPhone ?? "",
                Email = customer?.CustomerEmail ?? user.Email,

                // Lấy 3 đơn hàng gần nhất
                RecentOrders = customer?.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(3)
                    .Select(o => new OrderSummaryViewModel
                    {
                        OrderID = o.OrderID,
                        OrderCode = $"#DH{o.OrderID:0000}",
                        OrderDate = o.OrderDate ?? DateTime.Now,
                        TotalAmount = o.TotalAmount,
                        Status = o.OrderStatus
                    })
                    .ToList() ?? new List<OrderSummaryViewModel>(),

                // Vouchers (nếu có trong DB)
                Vouchers = new List<VoucherViewModel>
                {
                    new VoucherViewModel
                    {
                        Code = "EMAIL_888A28DF",
                        Title = "Ưu đãi khách hàng",
                        Description = "Giảm 10%, tối đa 100.000đ",
                        ExpiryDate = new DateTime(2025, 10, 25)
                    }
                },

                // Sản phẩm yêu thích (nếu có trong DB)
                FavoriteProducts = new List<ProductViewModel>()
            };

            return View(model);
        }

        // ==========================================
        // GET: /Users/History - LỊCH SỬ MUA HÀNG
        // ==========================================
        public ActionResult History(string status = "all", DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem lịch sử đơn hàng!";
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // Lấy thông tin customer
            var customer = db.Customers.FirstOrDefault(c => c.UserID == userId);

            if (customer == null)
            {
                ViewBag.CustomerName = "Khách hàng";
                ViewBag.Phone = "";
                ViewBag.UpdateDate = DateTime.Now.ToString("dd/MM/yyyy");
                ViewBag.TotalOrders = 0;
                ViewBag.TotalSpent = "0₫";
            }
            else
            {
                ViewBag.CustomerName = customer.CustomerName ?? "Khách hàng";
                ViewBag.Phone = customer.CustomerPhone ?? "";
                ViewBag.UpdateDate = customer.CreatedAt?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy");

                // Tính tổng số đơn hàng
                var totalOrders = db.Orders.Count(o => o.CustomerID == customer.CustomerID);
                ViewBag.TotalOrders = totalOrders;

                // Tính tổng tiền đã chi tiêu (trừ đơn đã hủy)
                var totalSpent = db.Orders
                    .Where(o => o.CustomerID == customer.CustomerID
                             && o.OrderStatus != "Đã hủy"
                             && o.OrderStatus != "Đã huỷ")
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0;
                ViewBag.TotalSpent = totalSpent.ToString("N0") + "₫";
            }

            // Query cơ bản: lấy tất cả đơn hàng của user
            var ordersQuery = db.Orders
                .Where(o => o.Customer.UserID == userId)
                .AsQueryable();

            // Lọc theo trạng thái
            if (status != "all")
            {
                if (status == "Đã thanh toán")
                {
                    // ✅ Lọc theo PaymentStatus cho "Đã thanh toán"
                    ordersQuery = ordersQuery.Where(o => o.PaymentStatus == "Đã thanh toán");
                }
                else if (status == "Đã giao")
                {
                    // Lọc theo OrderStatus cho "Đã giao" hoặc "Đã giao hàng"
                    ordersQuery = ordersQuery.Where(o =>
                        o.OrderStatus == "Đã giao" ||
                        o.OrderStatus == "Đã giao hàng");
                }
                else if (status == "Đã hủy")
                {
                    // Lọc theo OrderStatus cho "Đã hủy"
                    ordersQuery = ordersQuery.Where(o =>
                        o.OrderStatus == "Đã hủy" ||
                        o.OrderStatus == "Đã huỷ");
                }
                else
                {
                    // Các trạng thái khác (Chờ xử lý, Đang xử lý, v.v.)
                    ordersQuery = ordersQuery.Where(o => o.OrderStatus == status);
                }
            }

            // Lọc theo ngày
            if (fromDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                // Lấy đến hết ngày
                var endDate = toDate.Value.AddDays(1).AddSeconds(-1);
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= endDate);
            }

            // Map sang ViewModel
            var orders = ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToList()
                .Select(o => new OrderSummaryViewModel
                {
                    OrderID = o.OrderID,
                    OrderCode = "DH" + o.OrderID.ToString("D6"),
                    OrderDate = o.OrderDate ?? DateTime.Now,
                    TotalAmount = o.TotalAmount,
                    // ✅ Hiển thị đúng trạng thái tùy theo filter
                    Status = status == "Đã thanh toán" ? o.PaymentStatus : o.OrderStatus,
                    StatusText = status == "Đã thanh toán" ? o.PaymentStatus : o.OrderStatus
                })
                .ToList();

            // Truyền dữ liệu sang View
            ViewBag.CurrentStatus = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(orders);
        }

        // ==========================================
        // GET: /Users/Info - THÔNG TIN TÀI KHOẢN
        // ==========================================
        public ActionResult Info()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            SetKPIData();

            int userId = (int)Session["UserID"];
            var user = db.UserAccounts.Find(userId);
            var customer = db.Customers.FirstOrDefault(c => c.UserID == userId);

            var model = new ProfileInfoViewModel
            {
                UserID = user.UserID,
                FullName = customer?.CustomerName ?? user.Username,
                Phone = customer?.CustomerPhone ?? "",
                Email = customer?.CustomerEmail ?? user.Email,
                Gender = "", // Thêm field nếu có trong DB
                BirthDate = null, // Thêm field nếu có trong DB
                DefaultAddress = customer?.CustomerAddress ?? "",

                // Danh sách địa chỉ (nếu có table riêng)
                Addresses = new List<AddressViewModel>(),

                PasswordLastUpdated = "07/10/2025 13:15", // Lấy từ DB nếu có
                IsGoogleLinked = false, // Lấy từ DB nếu có
                IsZaloLinked = false // Lấy từ DB nếu có
            };

            return View(model);
        }

        // ==========================================
        // POST: /Users/UpdateInfo - CẬP NHẬT THÔNG TIN
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInfo(ProfileInfoViewModel model)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                SetKPIData();
                return View("Info", model);
            }

            try
            {
                int userId = (int)Session["UserID"];
                var user = db.UserAccounts.Find(userId);
                var customer = db.Customers.FirstOrDefault(c => c.UserID == userId);

                if (customer == null)
                {
                    // Tạo mới customer nếu chưa có
                    customer = new Customer
                    {
                        UserID = userId,
                        CustomerName = model.FullName,
                        CustomerPhone = model.Phone,
                        CustomerEmail = model.Email,
                        CustomerAddress = model.DefaultAddress,
                        CreatedAt = DateTime.Now
                    };
                    db.Customers.Add(customer);
                }
                else
                {
                    // Cập nhật thông tin
                    customer.CustomerName = model.FullName;
                    customer.CustomerPhone = model.Phone;
                    customer.CustomerEmail = model.Email;
                    customer.CustomerAddress = model.DefaultAddress;
                }

                // Cập nhật email trong UserAccount
                if (user != null)
                {
                    user.Email = model.Email;
                }

                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Info");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                SetKPIData();
                return View("Info", model);
            }
        }

        // ==========================================
        // POST: /Users/ChangePassword - ĐỔI MẬT KHẨU
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string OldPassword, string NewPassword)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];
            var user = db.UserAccounts.Find(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng!";
                return RedirectToAction("Info");
            }

            string oldHash = HashPassword(OldPassword);
            if (user.PasswordHash != oldHash)
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng!";
                return RedirectToAction("Info");
            }

            user.PasswordHash = HashPassword(NewPassword);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Info");
        }

        // ==========================================
        // GET: /Users/OrderDetail - CHI TIẾT ĐỔN HÀNG
        // ==========================================
        public ActionResult OrderDetail(int id)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            SetKPIData();

            int userId = (int)Session["UserID"];
            var order = db.Orders
                .Include("OrderDetails.Product")
                .Include("Customer")
                .FirstOrDefault(o => o.OrderID == id);

            if (order == null || order.Customer.UserID != userId)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("History");
            }

            var model = new OrderDetailViewModel
            {
                OrderID = order.OrderID,
                OrderCode = $"#DH{order.OrderID:0000}",
                OrderDate = order.OrderDate ?? DateTime.Now,
                TotalAmount = order.TotalAmount,
                Status = order.OrderStatus,
                DeliveryAddress = order.DeliveryAddress,
                PaymentStatus = order.PaymentStatus,
                Items = order.OrderDetails.Select(od => new OrderItemViewModel
                {
                    ProductName = od.Product.ProductName,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    Subtotal = od.Quantity * od.UnitPrice
                }).ToList()
            };

            return View(model);
        }

        // Helper methods
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private string GetStatusText(string status)
        {
            switch (status?.ToLower())
            {
                case "chờ xử lý": return "Chờ xác nhận";
                case "đã xác nhận": return "Đã xác nhận";
                case "đang vận chuyển": return "Đang vận chuyển";
                case "đã giao hàng": return "Đã giao hàng";
                case "đã hủy": return "Đã huỷ";
                default: return status ?? "";
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (Session["UserID"] != null)
            {
                int userId = (int)Session["UserID"];
                var cart = db.Carts.FirstOrDefault(c => c.UserID == userId && c.IsCheckedOut == false);
                ViewBag.CartCount = cart?.CartItems.Sum(ci => ci.Quantity) ?? 0;

                var user = db.UserAccounts.Find(userId);
                Session["Username"] = user?.Username;
                Session["AvatarPath"] = user?.AvatarPath ?? Url.Content("~/Image/icon.jpg");
            }
        }
    }
}
