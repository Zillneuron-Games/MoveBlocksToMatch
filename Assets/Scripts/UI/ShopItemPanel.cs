using TMPro;
using UnityEngine;
using Zillneuron.UILayout;

public class ShopItemPanel : AFragmentView
{
    private string id;
    private int count;
    private string title;
    private string price;
    private IProductPurchaser productPurchaser;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text priceText;

    public void SetUp(string id, string title, string price, IProductPurchaser productPurchaser)
    {
        SetUp(id, 0, title, price, productPurchaser);
    }

    public void SetUp(int count, string title, string price, IProductPurchaser productPurchaser)
    {
        SetUp(null, count, title, price, productPurchaser);
    }

    private void SetUp(string id, int count, string title, string price, IProductPurchaser productPurchaser)
    {
        this.id = id;
        this.count = count;
        this.title = title;
        this.price = price;
        this.productPurchaser = productPurchaser;
    }

    protected override void SetTexts()
    {
        base.SetTexts();

        titleText.text = title;
        priceText.text = price;
    }

    public void OnClick_Buy()
    {
        if(string.IsNullOrEmpty(id))
        {
            productPurchaser.GetFromAds(count);
        }
        else
        {
            productPurchaser.BuyFromStore(id);
        }        
    }
}

