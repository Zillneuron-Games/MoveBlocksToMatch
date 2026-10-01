using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using Zillneuron.InAppPurchase;
using Zillneuron.UILayout;

public class ShopDialog : ADialog
{
    private IPurchaser purchaser;
    private List<InAppProduct> products;

    [SerializeField]
    private ShopItemPanel[] shopItems;

    [SerializeField]
    private TMP_Text restorePurchaseText;

    public static ADialogParameters CreateDialogParameters(IPurchaser purchaser)
    {
        return new ShopDialogParameters(purchaser);
    }

    public override void SetUp(ADialogParameters parameters)
    {
        ShopDialogParameters currentParameters = parameters as ShopDialogParameters;

        purchaser = currentParameters.Purchaser;
        products = currentParameters.Purchaser.GetProducts();

        string adsItemTitle = string.Format(LocalizationManager.Instance.GetPhrase(PhraseCollection.AdsItemTitle), purchaser.FromAdsProductsCount);
        string adsItemPrice = LocalizationManager.Instance.GetPhrase(PhraseCollection.AdsItemPrice);
        shopItems[0].SetUp(purchaser.FromAdsProductsCount, adsItemTitle, adsItemPrice, purchaser);

        for (int i = 1; i < products.Count; i++)
        {
            Product product = products[i].ProductFromAppStore;
            shopItems[i].SetUp(product.definition.id, CleanLocalizedTitle(product.metadata.localizedTitle), product.metadata.localizedPriceString, purchaser);
        }
    }

    protected override void ConstructFragments()
    {
        base.ConstructFragments();

        for (int i = 0; i < shopItems.Length; i++)
        {
            shopItems[i].Construct();
        }
    }

    protected override void LaunchFragments()
    {
        base.LaunchFragments();

        for (int i = 0; i < shopItems.Length; i++)
        {
            shopItems[i].Launch();
        }
    }

    protected override void FinishFragments()
    {
        base.FinishFragments();

        for (int i = 0; i < shopItems.Length; i++)
        {
            shopItems[i].Finish();
        }
    }

    protected override void DestructFragments()
    {
        base.DestructFragments();

        for (int i = 0; i < shopItems.Length; i++)
        {
            shopItems[i].Destruct();
        }
    }

    protected override void OnLaunch()
    {
        base.OnLaunch();

        gameObject.SetActive(true);
    }

    protected override void OnFinish()
    {
        base.OnFinish();

        gameObject.SetActive(false);
    }
    
    protected override void SetTexts()
    {
        base.SetTexts();

        restorePurchaseText.text = LocalizationManager.Instance.GetPhrase(PhraseCollection.RestorePurchase);
    }

    private string CleanLocalizedTitle(string rawTitle)
    {
        // Google Play often appends "(App Name)" to the title.
        // This strips out "(Your Game Name)" if present.
        int bracketIndex = rawTitle.LastIndexOf('(');
        if (bracketIndex > 0)
        {
            return rawTitle.Substring(0, bracketIndex).Trim();
        }
        return rawTitle;
    }

    public void OnClick_RestorePurchase()
    {
       purchaser.RestorePurchases();
    }

    public void OnClick_Close()
    {
        ViewContext.Instance.HideDialog<ShopDialog>();
    }
}
