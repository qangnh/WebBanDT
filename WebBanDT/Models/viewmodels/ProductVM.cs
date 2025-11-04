using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanDT.Models.ViewModels
{
    public class ProductVM
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }

        public decimal ProductPrice { get; set; }
        public decimal? OldPrice { get; set; }

        public int? StockQuantity { get; set; }
        public string ProductImage { get; set; }

        // Ảnh phụ
        public List<string> ProductImages { get; set; } = new List<string>();

        public DateTime? CreatedAt { get; set; }
        public bool? IsActive { get; set; }

        public string CategoryName { get; set; }

        // Các thuộc tính bổ sung cho view
        public double Rating { get; set; } = 0.0;
        public int ReviewCount { get; set; } = 0;
        public int SoldQuantity { get; set; } = 0;

        // 🔹 Thông số kỹ thuật
        public string Specifications { get; set; }

        // 🔹 Danh sách sản phẩm liên quan
        public List<ProductVM> RelatedProducts { get; set; } = new List<ProductVM>();
    }
}