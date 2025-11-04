using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebBanDT.Models.ViewModels
{
    [MetadataType(typeof(CategoryMetadata))]
    public partial class Category { }

    [MetadataType(typeof(ProductMetadata))]
    public partial class Product { }

    [MetadataType(typeof(UserAccountMetadata))]
    public partial class UserAccount { }

    [MetadataType(typeof(OrderMetadata))]
    public partial class Order { }

    [MetadataType(typeof(OrderDetailMetadata))]
    public partial class OrderDetail { }

    [MetadataType(typeof(CustomerMetadata))]
    public partial class Customer { }

    [MetadataType(typeof(CartMetadata))]
    public partial class Cart { }

    [MetadataType(typeof(CartItemMetadata))]
    public partial class CartItem { }

}