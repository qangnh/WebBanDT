using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebBanDT.Models;

namespace WebBanDT.Models.viewmodels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string DeliveryAddress { get; set; }

        [Display(Name = "Ghi chú đơn hàng")]
        public string OrderNote { get; set; }  // Thêm để bind textarea

        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        public Cart Cart { get; set; } // để hiển thị giỏ hàng
        public List<CartItem> CartItems { get; set; }

    }
}
