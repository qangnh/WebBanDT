using System.Collections.Generic;
using WebBanDT.Models;

namespace WebBanDT.Models.ViewModels
{
    public class CustomerHomeVM
    {
        public List<ProductVM> Phones { get; set; }
        public List<ProductVM> Laptops { get; set; }
        public List<ProductVM> Tablets { get; set; }
        public List<ProductVM> Watches { get; set; }
        public List<ProductVM> Sounds { get; set; }
        public List<ProductVM> Screens { get; set; }
    }
}
