using System.Collections.Generic;
using UnityEngine;
using Zillneuron.InAppPurchase;

[CreateAssetMenu(fileName = "InAppPurchaseSettings", menuName = "ScriptableObjects/InAppPurchaseSettings", order = 1)]
public class InAppPurchaseSettingsScriptableObject : AInAppPurchaseSettingsScriptableObject
{
    public override List<InAppProductMetadata> GetProducts()
    {
        IReadOnlyList<StoreProductSettings> catalog = SettingsManager.Instance.Settings.StoreProducts;
        var products = new List<InAppProductMetadata>(catalog.Count);
        foreach (StoreProductSettings product in catalog)
        {
            products.Add(new InAppProductMetadata(product.ProductId, product.PurchaseType));
        }

        return products;
    }
}
