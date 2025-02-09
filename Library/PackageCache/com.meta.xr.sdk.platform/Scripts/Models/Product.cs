// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  /// The class that represents the product information for a specific IAP which
  /// is available for purchase in your app. You can retrieve more information
  /// about the product(s) by using their SKU with IAP.GetProductsBySKU()
  public class Product
  {
    /// The description for the product. The description should be meaningful and
    /// explanatory to help outline the product and its features.
    public readonly string Description;
    /// The formatted string for the Models.Price. This is the same value stored in
    /// Models.Price.
    public readonly string FormattedPrice;
    /// The name of the product. This will be used as a the display name and should
    /// be aligned with the user facing title.
    public readonly string Name;
    /// The Models.Price of the product contains the currency code, the amount in
    /// hundredths, and the formatted string representation.
    public readonly Price Price;
    /// The unique string that you use to reference the product in your app. The
    /// SKU is case-sensitive and should match the SKU reference in your code.
    public readonly string Sku;
    /// The type of product. An In-app purchase (IAP) add-on can be
    /// ProductType.DURABLE, ProductType.CONSUMABLE, or a ProductType.SUBSCRIPTION.
    public readonly ProductType Type;


    public Product(IntPtr o)
    {
      Description = CAPI.ovr_Product_GetDescription(o);
      FormattedPrice = CAPI.ovr_Product_GetFormattedPrice(o);
      Name = CAPI.ovr_Product_GetName(o);
      Price = new Price(CAPI.ovr_Product_GetPrice(o));
      Sku = CAPI.ovr_Product_GetSKU(o);
      Type = CAPI.ovr_Product_GetType(o);
    }
  }

  /// Represents a paginated list of Models.Product elements. It allows you to
  /// easily access and manipulate the elements in the paginated list, such as
  /// the size of the list and if there is a next page of elements available.
  public class ProductList : DeserializableList<Product> {
    /// Instantiates a C# wrapper class that wraps a native list by pointer. Used internally by Platform SDK to wrap the list.
    public ProductList(IntPtr a) {
      var count = (int)CAPI.ovr_ProductArray_GetSize(a);
      _Data = new List<Product>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new Product(CAPI.ovr_ProductArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_ProductArray_GetNextUrl(a);
    }

  }
}
