using System.Collections.Generic;
using UnityEngine;
using Zillneuron.InAppPurchase;

public interface IPurchaser : IProductPurchaser
{
    void RestorePurchases();

    List<InAppProduct> GetProducts();

    int FromAdsProductsCount { get; }
}
