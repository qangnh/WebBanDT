using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanDT.Models.viewmodels
{
    public class CategoryWithBrandsViewModel
    {
        public Category Category { get; set; }

        // Nhập dạng: "iPhone, Samsung, Xiaomi"
        public string BrandNames { get; set; }
    }
}