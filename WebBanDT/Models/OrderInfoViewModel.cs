using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebBanDT.Models
{
    public class OrderInfoViewModel
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Note { get; set; }
        public string DeliveryMethod { get; set; }
        public bool ChinhSach { get; set; }
    }
}
