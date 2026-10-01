using System.Collections.Generic;
using UnityEngine;
using Zillneuron.Ads;
using Zillneuron.InAppPurchase;

public class PurchaseManager : MonoBehaviour, IPurchaser
{
    private int currentProductsCount;

    [SerializeField]
    private int fromAdsProductsCount;

    public int FromAdsProductsCount { get { return fromAdsProductsCount; } }

    public List<InAppProduct> GetProducts()
    {
        return InAppPurchaseService.Instance.GetProducts();
    }

    public void RestorePurchases()
    {
        InAppPurchaseService.Instance.RestorePurchases(RestorePurchasesHandler);
    }

    private void RestorePurchasesHandler(ERestorePurchaseResult result)
    {
        Debug.LogError($"Restore Purchases Result: {result}");
    }

    public void BuyFromStore(string productId)
    {
        InAppPurchaseService.Instance.BuyProduct(productId, BuyFromStoreHandler);
    }

    private void BuyFromStoreHandler(string productId, EBuyProductResult result, string message)
    {
        Debug.LogError($"Buy Product Result: {result}, Message: {message}, Product: {productId}");
    }

    public void GetFromAds(int count) 
    {
        currentProductsCount = count;

        AdsService.Instance.AddInterstitialNewEventRisedHandler(GetFromAdsHandler);
        AdsService.Instance.ShowInterstitial();
    }

    private void GetFromAdsHandler(EAdResult result, string message)
    {
        Debug.LogError($"Get From Ads Result: {result}, Message: {message}, Count: {currentProductsCount}");

        currentProductsCount = 0;

        AdsService.Instance.RemoveInterstitialNewEventRisedHandler(GetFromAdsHandler);
    }
}
