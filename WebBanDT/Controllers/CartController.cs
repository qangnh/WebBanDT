using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebBanDT.Models;
using WebBanDT.Models.viewmodels;

namespace WebBanDT.Controllers
{
    public class CartController : Controller
    {
        private WebBanDTEntities db = new WebBanDTEntities();

        // 🛒 Hiển thị giỏ hàng
        public ActionResult GioHang()
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn chưa đăng nhập! Vui lòng đăng nhập để xem giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);
            var cart = db.Carts.FirstOrDefault(c => c.UserID == userId && (c.IsCheckedOut == false || c.IsCheckedOut == null));

            if (cart == null)
            {
                ViewBag.Message = "Giỏ hàng của bạn hiện đang trống.";
                ViewBag.CartCount = 0; // ⭐ Thêm dòng này
                return View(new List<CartItem>());
            }

            var cartItems = db.CartItems
                              .Include(ci => ci.Product)
                              .Include(ci => ci.ProductVersion)
                              .Include(ci => ci.ProductColor)
                              .Where(ci => ci.CartID == cart.CartID)
                              .ToList();

            // ⭐ CẬP NHẬT SỐ LƯỢNG GIỎ HÀNG
            ViewBag.CartCount = cartItems.Sum(c => c.Quantity);

            return View(cartItems);
        }


        // ➕ Thêm sản phẩm vào giỏ
        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1, int? versionId = null, int? colorId = null)
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thêm sản phẩm!";
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            var product = db.Products.Find(productId);
            if (product == null || (product.IsActive.HasValue && !product.IsActive.Value))
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã bị vô hiệu.";
                return RedirectToAction("Index", "CustomerHome");
            }

            // ✅ Tính đúng giá theo phiên bản
            decimal unitPrice = product.ProductPrice;
            if (versionId.HasValue)
            {
                var ver = db.ProductVersions.FirstOrDefault(v => v.VersionID == versionId.Value
                                                                 && v.ProductID == productId);
                if (ver != null && ver.VersionPrice.HasValue)
                {
                    unitPrice = ver.VersionPrice.Value;
                }
            }

            // Lấy / tạo giỏ
            var cart = db.Carts.FirstOrDefault(c =>
                c.UserID.HasValue && c.UserID.Value == userId &&
                (c.IsCheckedOut == false || c.IsCheckedOut == null));

            if (cart == null)
            {
                cart = new Cart
                {
                    UserID = userId,
                    CreatedAt = DateTime.Now,
                    IsCheckedOut = false
                };
                db.Carts.Add(cart);
                db.SaveChanges();
            }

            // ✅ Ghép theo: product + version + color
            var cartItem = db.CartItems.FirstOrDefault(ci =>
                ci.CartID == cart.CartID &&
                ci.ProductID == productId &&
                ci.VersionID == versionId &&
                ci.ColorID == colorId
            );

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartID = cart.CartID,
                    ProductID = productId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    AddedAt = DateTime.Now,
                    VersionID = versionId,
                    ColorID = colorId
                };
                db.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
                cartItem.AddedAt = DateTime.Now;
                cartItem.UnitPrice = unitPrice; // nếu muốn update luôn giá mới
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = $"Bạn đã thêm sản phẩm \"{product.ProductName}\" vào giỏ hàng!";
            return RedirectToAction("GioHang");
        }


        // 🛒 MUA NGAY 1 SẢN PHẨM TRONG GIỎ HÀNG
        [HttpPost]
        public ActionResult BuyFromCart(int cartItemId)
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để mua sản phẩm!";
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // lấy cartItem + Product + kiểm tra có thuộc giỏ của user không
            var cartItem = db.CartItems
                             .Include(ci => ci.Product)
                             .Include(ci => ci.Cart)
                             .FirstOrDefault(ci =>
                                 ci.CartItemID == cartItemId &&
                                 ci.Cart.UserID == userId &&
                                 (ci.Cart.IsCheckedOut ?? false) == false);

            if (cartItem == null || cartItem.Product == null || !(cartItem.Product.IsActive ?? true))
            {
                TempData["ErrorMessage"] = "Sản phẩm không hợp lệ hoặc đã bị xóa!";
                return RedirectToAction("GioHang");
            }

            // Tạo 1 Cart tạm thời chỉ chứa item này để đưa sang Order/ThongTin
            var cartMuaNgay = new Cart
            {
                UserID = userId,
                CreatedAt = DateTime.Now,
                IsCheckedOut = false,
                CartItems = new List<CartItem>
        {
            new CartItem
            {
                ProductID  = cartItem.ProductID,
                Quantity   = cartItem.Quantity,
                UnitPrice  = cartItem.UnitPrice,   // ✔ dùng giá đã lưu trong giỏ
                Product    = cartItem.Product,
                AddedAt    = DateTime.Now,
                VersionID  = cartItem.VersionID,   // ✔ giữ phiên bản
                ColorID    = cartItem.ColorID      // ✔ giữ màu
            }
        }
            };

            // Lưu vào session dùng chung với BuyAll / BuySelected
            Session["Cart"] = cartMuaNgay;

            // Chuyển sang trang thông tin đặt hàng
            return RedirectToAction("ThongTin", "Order");
        }

        // 🛍️ Mua ngay
        [HttpPost]
        public ActionResult BuyNow(int productId, int quantity = 1, int? versionId = null, int? colorId = null)
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để mua sản phẩm!";
                return RedirectToAction("Login", "Account", new { returnUrl = Request.RawUrl });
            }

            int userId = Convert.ToInt32(Session["UserID"]);
            var product = db.Products.Find(productId);

            if (product == null || (product.IsActive.HasValue && !product.IsActive.Value))
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã bị vô hiệu.";
                return RedirectToAction("Index", "CustomerHome");
            }

            // ✅ Tính giá theo phiên bản
            decimal unitPrice = product.ProductPrice;
            if (versionId.HasValue)
            {
                var ver = db.ProductVersions.FirstOrDefault(v => v.VersionID == versionId.Value
                                                                 && v.ProductID == productId);
                if (ver != null && ver.VersionPrice.HasValue)
                {
                    unitPrice = ver.VersionPrice.Value;
                }
            }

            var cart = new Cart
            {
                UserID = userId,
                CreatedAt = DateTime.Now,
                IsCheckedOut = false,
                CartItems = new List<CartItem>
        {
            new CartItem
            {
                ProductID = product.ProductID,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Product = product,
                AddedAt = DateTime.Now,
                VersionID = versionId,
                ColorID = colorId
            }
        }
            };

            Session["Cart"] = cart;
            return RedirectToAction("ThongTin", "Order");
        }


        // 🛒 MUA TẤT CẢ SẢN PHẨM TRONG GIỎ HÀNG
        [HttpPost]
        public ActionResult BuyAll()
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập!";
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            var cart = db.Carts.FirstOrDefault(c => c.UserID == userId && (c.IsCheckedOut == false || c.IsCheckedOut == null));

            if (cart == null)
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn trống!";
                return RedirectToAction("GioHang");
            }

            var cartItems = db.CartItems
                              .Include(ci => ci.Product)
                              .Where(ci => ci.CartID == cart.CartID)
                              .ToList();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn trống!";
                return RedirectToAction("GioHang");
            }

            var cartMuaNgay = new Cart
            {
                UserID = userId,
                CreatedAt = DateTime.Now,
                IsCheckedOut = false,
                CartItems = new List<CartItem>()
            };

            foreach (var item in cartItems)
            {
                var product = db.Products.Find(item.ProductID);

                if (product != null && (!product.IsActive.HasValue || product.IsActive.Value))
                {
                    cartMuaNgay.CartItems.Add(new CartItem
                    {
                        ProductID = product.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,          // ✅ dùng giá đã lưu trong cart
                        Product = product,
                        AddedAt = DateTime.Now,
                        VersionID = item.VersionID,          // ✅ giữ đúng version
                        ColorID = item.ColorID               // ✅ giữ đúng color
                    });
                }
            }


            if (!cartMuaNgay.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm hợp lệ để mua!";
                return RedirectToAction("GioHang");
            }

            Session["Cart"] = cartMuaNgay;
            return RedirectToAction("ThongTin", "Order");
        }

        // 🛒 MUA CÁC SẢN PHẨM ĐÃ CHỌN (với checkbox)
        [HttpPost]
        public ActionResult BuySelected(string selectedItems)
        {
            System.Diagnostics.Debug.WriteLine($"BuySelected called with: {selectedItems}");

            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập!";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(selectedItems))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm!";
                return RedirectToAction("GioHang");
            }

            // Parse string thành List<int>
            var selectedItemIds = selectedItems.Split(',')
                                               .Select(id => int.Parse(id.Trim()))
                                               .ToList();

            System.Diagnostics.Debug.WriteLine($"Parsed IDs: {string.Join(", ", selectedItemIds)}");

            int userId = Convert.ToInt32(Session["UserID"]);

            var userCart = db.Carts
                             .Include(c => c.CartItems.Select(ci => ci.Product))
                             .FirstOrDefault(c => c.UserID == userId && (c.IsCheckedOut ?? false) == false);

            if (userCart == null || userCart.CartItems == null)
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống!";
                return RedirectToAction("GioHang");
            }

            var selectedCartItems = userCart.CartItems
                                            .Where(ci => selectedItemIds.Contains(ci.CartItemID))
                                            .ToList();

            System.Diagnostics.Debug.WriteLine($"Found {selectedCartItems.Count} items");

            var cartMuaNgay = new Cart
            {
                UserID = userId,
                CreatedAt = DateTime.Now,
                IsCheckedOut = false,
                CartItems = new List<CartItem>()
            };

            foreach (var cartItem in selectedCartItems)
            {
                if (cartItem.Product != null && (cartItem.Product.IsActive ?? true))
                {
                    cartMuaNgay.CartItems.Add(new CartItem
                    {
                        ProductID = cartItem.Product.ProductID,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice,       // ✅ không lấy lại từ Product
                        Product = cartItem.Product,
                        AddedAt = DateTime.Now,
                        VersionID = cartItem.VersionID,       // ✅
                        ColorID = cartItem.ColorID            // ✅
                    });
                }
            }


            if (!cartMuaNgay.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Không có sản phẩm hợp lệ để mua!";
                return RedirectToAction("GioHang");
            }

            Session["Cart"] = cartMuaNgay;
            System.Diagnostics.Debug.WriteLine($"Saved {cartMuaNgay.CartItems.Count} items to Session");
            return RedirectToAction("ThongTin", "Order");
        }

        // 🔄 Cập nhật số lượng
        [HttpPost]
        public ActionResult UpdateQuantity(int cartItemId, int quantity)
        {
            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để cập nhật giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            var item = db.CartItems.Find(cartItemId);
            if (item != null)
            {
                if (quantity > 0) item.Quantity = quantity;
                else db.CartItems.Remove(item);
                db.SaveChanges();
            }
            return RedirectToAction("GioHang");
        }

        // ❌ Xóa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int cartItemId)
        {
            System.Diagnostics.Debug.WriteLine($"RemoveItem called with cartItemId: {cartItemId}");

            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xóa sản phẩm khỏi giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);
            var item = db.CartItems.Find(cartItemId);

            if (item != null)
            {
                // Kiểm tra item có thuộc về user hiện tại không
                var cart = db.Carts.FirstOrDefault(c => c.CartID == item.CartID && c.UserID == userId);

                if (cart != null)
                {
                    db.CartItems.Remove(item);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Sản phẩm đã được xóa khỏi giỏ hàng!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa sản phẩm này!";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại!";
            }

            return RedirectToAction("GioHang");
        }

        // ❌ XÓA NHIỀU SẢN PHẨM ĐÃ CHỌN
        [HttpPost]
        public ActionResult RemoveSelectedItems(string cartItemIds)
        {
            System.Diagnostics.Debug.WriteLine($"RemoveSelectedItems called with: {cartItemIds}");

            if (Session["UserID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xóa sản phẩm khỏi giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(cartItemIds))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm để xóa!";
                return RedirectToAction("GioHang");
            }

            // Parse string thành List<int>
            var itemIdList = cartItemIds.Split(',')
                                        .Select(id => int.Parse(id.Trim()))
                                        .ToList();

            System.Diagnostics.Debug.WriteLine($"Parsed IDs: {string.Join(", ", itemIdList)}");

            int userId = Convert.ToInt32(Session["UserID"]);
            int deletedCount = 0;

            foreach (var cartItemId in itemIdList)
            {
                var item = db.CartItems.Find(cartItemId);

                if (item != null)
                {
                    var cart = db.Carts.FirstOrDefault(c => c.CartID == item.CartID && c.UserID == userId);

                    if (cart != null)
                    {
                        db.CartItems.Remove(item);
                        deletedCount++;
                        System.Diagnostics.Debug.WriteLine($"Đã xóa CartItemID: {cartItemId}");
                    }
                }
            }

            if (deletedCount > 0)
            {
                try
                {
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Đã xóa {deletedCount} sản phẩm khỏi giỏ hàng!";
                    System.Diagnostics.Debug.WriteLine($"SaveChanges thành công! Đã xóa {deletedCount} sản phẩm");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa sản phẩm: " + ex.Message;
                    System.Diagnostics.Debug.WriteLine($"Lỗi SaveChanges: {ex.Message}");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào được xóa!";
            }

            return RedirectToAction("GioHang");
        }
        // ➕ Thêm sản phẩm vào giỏ (AJAX)
        [HttpPost]
        public JsonResult AddToCartAjax(int productId, int quantity = 1, int? versionId = null, int? colorId = null)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập để thêm sản phẩm!" });
                }

                int userId = Convert.ToInt32(Session["UserID"]);

                var product = db.Products.Find(productId);
                if (product == null || (product.IsActive.HasValue && !product.IsActive.Value))
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã bị vô hiệu." });
                }

                // Tính giá theo phiên bản
                decimal unitPrice = product.ProductPrice;
                if (versionId.HasValue)
                {
                    var ver = db.ProductVersions.FirstOrDefault(v => v.VersionID == versionId.Value && v.ProductID == productId);
                    if (ver != null && ver.VersionPrice.HasValue)
                    {
                        unitPrice = ver.VersionPrice.Value;
                    }
                }

                // Lấy / tạo giỏ
                var cart = db.Carts.FirstOrDefault(c =>
                    c.UserID.HasValue && c.UserID.Value == userId &&
                    (c.IsCheckedOut == false || c.IsCheckedOut == null));

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserID = userId,
                        CreatedAt = DateTime.Now,
                        IsCheckedOut = false
                    };
                    db.Carts.Add(cart);
                    db.SaveChanges();
                }

                // Ghép theo: product + version + color
                var cartItem = db.CartItems.FirstOrDefault(ci =>
                    ci.CartID == cart.CartID &&
                    ci.ProductID == productId &&
                    ci.VersionID == versionId &&
                    ci.ColorID == colorId
                );

                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = productId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        AddedAt = DateTime.Now,
                        VersionID = versionId,
                        ColorID = colorId
                    };
                    db.CartItems.Add(cartItem);
                }
                else
                {
                    cartItem.Quantity += quantity;
                    cartItem.AddedAt = DateTime.Now;
                    cartItem.UnitPrice = unitPrice;
                }

                db.SaveChanges();

                // Tính tổng số lượng trong giỏ
                int totalQuantity = db.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .Sum(ci => ci.Quantity);

                return Json(new
                {
                    success = true,
                    message = $"Đã thêm \"{product.ProductName}\" vào giỏ hàng!",
                    cartCount = totalQuantity
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

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