using System;
using System.Collections.Generic;
using UnityEngine;
using Zillneuron.Ads;
using Zillneuron.Blockmates.Services.SignIn;
using Zillneuron.InAppPurchase;

public class PurchaseManager : MonoBehaviour, IPurchaser
{
    private int currentProductsCount;
    private bool storePurchasePending;

    public int FromAdsProductsCount
    {
        get
        {
            return SettingsManager.Instance.Settings.FromAdsProductsCount;
        }
    }

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
        AccountService account = AccountService.Instance;
        if (storePurchasePending || !account.IsReady)
        {
            return;
        }

        StoreProductSettings product = FindStoreProduct(productId);
        if (product == null)
        {
            Debug.LogWarning("The requested product is missing from the store catalog.");
            return;
        }

        bool grantsHints = product.GrantAmount > 0;
        if (grantsHints && !account.TryBeginHintPurchase())
        {
            return;
        }

        storePurchasePending = true;
        try
        {
            InAppPurchaseService.Instance.BuyProduct(productId, (purchasedId, result, transactionId) =>
            {
                try
                {
                    if (result == EBuyProductResult.Succeeded && !string.IsNullOrWhiteSpace(transactionId) &&
                        string.Equals(purchasedId, product.ProductId, StringComparison.Ordinal))
                    {
                        if (grantsHints)
                        {
                            account.GrantPurchasedHints(product.GrantAmount, transactionId);
                        }
                    }
                    else if (result != EBuyProductResult.UserCancelled)
                    {
                        Debug.LogWarning($"Store purchase did not complete: {result} ({purchasedId}).");
                    }
                }
                finally
                {
                    FinishStorePurchase(account, grantsHints);
                }
            });
        }
        catch
        {
            FinishStorePurchase(account, grantsHints);
            throw;
        }
    }

    private void FinishStorePurchase(AccountService account, bool grantsHints)
    {
        storePurchasePending = false;
        if (grantsHints)
        {
            account.EndHintPurchase();
        }
    }

    private static StoreProductSettings FindStoreProduct(string productId)
    {
        foreach (StoreProductSettings product in SettingsManager.Instance.Settings.StoreProducts)
        {
            if (string.Equals(product.ProductId, productId, StringComparison.Ordinal))
            {
                return product;
            }
        }

        return null;
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
