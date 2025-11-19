using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDT.Models; // giả sử DbContext và entity ở đây
using WebBanDT.Models.viewmodels;

namespace WebBanDT.Controllers
{
    public class OrderController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        // GET: Order/ThongTin
        // GET: Order/ThongTin
        [HttpGet]
        public ActionResult ThongTin(int? productId, int? quantity)
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thanh toán!";
                return RedirectToAction("Login", "Account");
            }

            Cart cart = Session["Cart"] as Cart;

            // Nếu bấm Mua ngay từ Product Detail
            if (productId.HasValue)
            {
                var product = db.Products.Find(productId.Value);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại!";
                    return RedirectToAction("Index", "CustomerHome");
                }

                if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
                {
                    cart = new Cart
                    {
                        CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        Product = product,
                        ProductID = product.ProductID,
                        Quantity = quantity ?? 1,
                        UnitPrice = product.ProductPrice
                    }
                }
                    };
                    Session["Cart"] = cart;
                }
            }

            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống!";
                return RedirectToAction("GioHang", "Cart");
            }

            // 🔥 GÁN ẢNH THEO MÀU CHO CART TRONG SESSION
            foreach (var item in cart.CartItems)
            {
                if (item.Product == null) continue;

                // Nếu có ColorID => ưu tiên ảnh màu
                if (item.ColorID.HasValue)
                {
                    var color = db.ProductColors
                                 .FirstOrDefault(c => c.ColorID == item.ColorID.Value);

                    if (color != null && !string.IsNullOrEmpty(color.ColorImage))
                    {
                        // Chỉ đổi ảnh đang hiển thị, KHÔNG SaveChanges => DB không bị đổi
                        item.Product.ProductImage = color.ColorImage;
                    }
                }
            }

            var model = new CheckoutViewModel
            {
                Cart = cart,
                TotalAmount = cart.CartItems.Sum(i => i.Quantity * i.UnitPrice) // ✅ dùng UnitPrice
            };

            return View(model);
        }


        // POST: Order/ThongTin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThongTin(CheckoutViewModel model)
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thanh toán!";
                return RedirectToAction("Login", "Account");
            }

            var cart = Session["Cart"] as Cart;
            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống!";
                return RedirectToAction("GioHang", "Cart");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // Lấy hoặc tạo customer
            var customer = db.Customers.FirstOrDefault(c => c.UserID == userId);
            if (customer == null)
            {
                customer = new Customer
                {
                    UserID = userId,
                    CustomerName = model.FullName,
                    CustomerPhone = model.Phone,
                    CustomerAddress = model.DeliveryAddress,
                    CreatedAt = DateTime.Now
                };
                db.Customers.Add(customer);
                db.SaveChanges();
            }
            else
            {
                customer.CustomerName = model.FullName;
                customer.CustomerPhone = model.Phone;
                customer.CustomerAddress = model.DeliveryAddress;
                db.SaveChanges();
            }

            // Tạo order từ cart
            var order = new Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now,
                DeliveryAddress = model.DeliveryAddress,
                TotalAmount = cart.CartItems.Sum(i => i.UnitPrice * i.Quantity),
                PaymentStatus = "Chưa thanh toán",
                OrderStatus = "Chờ xử lý"
            };
            db.Orders.Add(order);
            db.SaveChanges();

            // Thêm chi tiết đơn hàng
            foreach (var item in cart.CartItems)
            {
                db.OrderDetails.Add(new OrderDetail
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,   // ✅ giá theo phiên bản đã lưu trong CartItem
                    VersionID = item.VersionID,   // ✅ lưu phiên bản
                    ColorID = item.ColorID      // ✅ lưu màu
                });
            }
            db.SaveChanges();


            // Xóa giỏ hàng tạm trong session
            Session["Cart"] = null;

            TempData["SuccessMessage"] = "Đặt hàng thành công!";

            return RedirectToAction("OrderSuccess", new { orderId = order.OrderID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MuaNgay(int productId, int quantity)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để mua hàng!";
                return RedirectToAction("Login", "Account");
            }

            // Tạo giỏ hàng tạm
            Cart cart = new Cart();

            // Gọi hàm AddItem (nếu có sẵn), hoặc tự thêm như sau:
            var product = db.Products.FirstOrDefault(p => p.ProductID == productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction("CustomerHome", "Home");
            }

            cart.CartItems = new List<CartItem>
{
    new CartItem
    {
        ProductID = product.ProductID,
        Product = product, // ✅ gán thẳng đối tượng Product
        UnitPrice = product.ProductPrice,
        Quantity = quantity
    }
};


            // Gán user & lưu vào session
            cart.UserID = Convert.ToInt32(Session["UserID"]);
            Session["Cart"] = cart;

            // Chuyển sang trang Thông tin thanh toán
            return RedirectToAction("ThongTin", "Order");
        }


        // GET: Order/Checkout
        [HttpGet]
        public ActionResult Checkout()
        {
            var cart = Session["Cart"] as Cart;
            if (cart == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống";
                return RedirectToAction("GioHang", "Cart");
            }

            var model = new CheckoutViewModel
            {
                TotalAmount = cart.CartItems.Sum(i => i.Product.ProductPrice * i.Quantity)
            };

            return View(model);
        }

        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin không hợp lệ.";
                return View(model);
            }

            var cart = Session["Cart"] as Cart;
            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("GioHang", "Cart");
            }

            var userIdObj = Session["UserID"];
            if (userIdObj == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "Account");
            }
            int userId = Convert.ToInt32(userIdObj);

            // =========== CUSTOMER ===========
            var customer = db.Customers.FirstOrDefault(c => c.UserID == userId);
            if (customer == null)
            {
                customer = new Customer
                {
                    UserID = userId,
                    CustomerName = model.FullName,
                    CustomerPhone = model.Phone,
                    CustomerAddress = model.DeliveryAddress,
                    CreatedAt = DateTime.Now
                };
                db.Customers.Add(customer);
                db.SaveChanges();
            }
            else
            {
                customer.CustomerName = model.FullName;
                customer.CustomerPhone = model.Phone;
                customer.CustomerAddress = model.DeliveryAddress;
                db.SaveChanges();
            }
            // ✅ TỔNG TIỀN DÙNG GIÁ ĐÃ CHỌN (UnitPrice)
            var subTotal = cart.CartItems.Sum(i => i.UnitPrice * i.Quantity);
            var vat = subTotal * 0.10m;           // 10% VAT
            var total = subTotal + vat;           // Tổng + VAT

            var order = new Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now,
                DeliveryAddress = model.DeliveryAddress,
                TotalAmount = total,                 // ✅ GIÁ ĐÃ CỘNG VAT
                PaymentStatus = "Chưa thanh toán",
                OrderStatus = "Chờ xử lý"
            };

            db.Orders.Add(order);
            db.SaveChanges();

            // ✅ LƯU CHI TIẾT ĐƠN HÀNG ĐÚNG PHIÊN BẢN + MÀU
            foreach (var item in cart.CartItems)
            {
                db.OrderDetails.Add(new OrderDetail
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,

                    UnitPrice = item.UnitPrice,   // ✅ giá theo phiên bản
                    VersionID = item.VersionID,   // ✅ lưu phiên bản
                    ColorID = item.ColorID      // ✅ lưu màu
                });
            }
            db.SaveChanges();

            // =========== DỌN GIỎ HÀNG DB + SESSION ===========
            var oldCart = db.Carts.FirstOrDefault(c =>
                c.UserID.HasValue && c.UserID.Value == userId &&
                (c.IsCheckedOut == false || c.IsCheckedOut == null));

            if (oldCart != null)
            {
                var oldItems = db.CartItems.Where(ci => ci.CartID == oldCart.CartID).ToList();
                if (oldItems.Any())
                {
                    db.CartItems.RemoveRange(oldItems);
                }

                oldCart.IsCheckedOut = true;
                db.SaveChanges();
            }

            Session["Cart"] = null;
            TempData["SuccessMessage"] = "Đặt hàng thành công! Giỏ hàng đã được làm trống.";

            return RedirectToAction("OrderSuccess", new { orderId = order.OrderID });
        }




        // GET: Order/OrderSuccess
        [HttpGet]
        public ActionResult OrderSuccess(int orderId)
        {
            var order = db.Orders
                          .Include(o => o.Customer)
                          .Include(o => o.OrderDetails.Select(od => od.Product))
                          .Include(o => o.OrderDetails.Select(od => od.ProductVersion)) // ✅
                          .Include(o => o.OrderDetails.Select(od => od.ProductColor))   // ✅
                          .FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index", "CustomerHome");
            }

            return View(order);
        }


        [HttpGet]
        public ActionResult LichSuDonHang()
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem lịch sử đơn hàng!";
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // Lấy danh sách đơn hàng của người dùng, kèm chi tiết sản phẩm
            var orders = db.Orders
                           .Where(o => o.Customer.UserID == userId)
                           .Include("Customer")
                           .Include("OrderDetails.Product")
                           .OrderByDescending(o => o.OrderDate)
                           .ToList();

            return View(orders);
        }
    }
}
